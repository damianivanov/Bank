using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Bank.Tests.Infrastructure;

// Истински UserManager<User>/RoleManager<Role> над InMemory доставчика. Identity EF хранилищата работят
// върху InMemory, затова можем да засеем потребители, роли и UserRole връзки и да тестваме реалната услуга.
internal sealed class IdentityTestHarness : IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    private IdentityTestHarness(ServiceProvider serviceProvider, IServiceScope scope, AppDbContext dbContext)
    {
        this.serviceProvider = serviceProvider;
        this.scope = scope;
        DbContext = dbContext;
        UserManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        RoleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    }

    public AppDbContext DbContext { get; }
    public UserManager<User> UserManager { get; }
    public RoleManager<Role> RoleManager { get; }

    public static IdentityTestHarness Create(string databaseName)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddDbContext<AppDbContext>(options => options
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

        services
            .AddIdentity<User, Role>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();

        return new IdentityTestHarness(serviceProvider, scope, dbContext);
    }

    public async Task EnsureRolesAsync(params string[] roleNames)
    {
        foreach (var roleName in roleNames)
        {
            if (!await RoleManager.RoleExistsAsync(roleName))
            {
                await RoleManager.CreateAsync(new Role
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                });
            }
        }
    }

    // Засява потребител с подадените роли. По избор го свързва с лице (PersonId) през създаване на Person.
    public async Task<User> SeedUserAsync(
        string email,
        string? firstName,
        string? lastName,
        DateTime dateCreated,
        bool isActive = true,
        bool linkPerson = false,
        string? personFirstName = null,
        string? personLastName = null,
        string[]? roleNames = null)
    {
        roleNames ??= [];
        await EnsureRolesAsync(RoleNames.User, RoleNames.Staff, RoleNames.Admin, RoleNames.Customer);

        long? personId = null;
        if (linkPerson)
        {
            var person = new Person
            {
                FirstName = personFirstName ?? firstName ?? "Лице",
                LastName = personLastName ?? lastName ?? "Лицев",
                Egn = Guid.NewGuid().ToString("N")[..10],
            };
            DbContext.Persons.Add(person);
            await DbContext.SaveChangesAsync();
            personId = person.Id;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            DateCreated = dateCreated,
            PersonId = personId,
        };

        var createResult = await UserManager.CreateAsync(user, "Password1");
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        if (roleNames.Length > 0)
        {
            await UserManager.AddToRolesAsync(user, roleNames);
        }

        return user;
    }

    public ClaimsPrincipal BuildPrincipal(User user, bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, RoleNames.Admin));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.NameIdentifier, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        scope.Dispose();
        await serviceProvider.DisposeAsync();
    }
}
