using Bank.Core.Enums;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Infrastructure;

public static class ApplicationBuilder
{
    public static void MigrateDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }

    public static async Task SeedDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var roleName in new[] { RoleNames.User, RoleNames.Customer, RoleNames.Staff, RoleNames.Admin })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            }
        }

        await SeedCreditTypeConditionsAsync(dbContext);

        await SeedAdminUserAsync(configuration, userManager);
    }

    private static async Task SeedCreditTypeConditionsAsync(AppDbContext dbContext)
    {
        var seedConditions = new[]
        {
            new CreditTypeCondition
            {
                CreditType = CreditType.Consumer,
                Name = "Consumer",
                StandardAnnualInterestRate = 8.50m,
                VipAnnualInterestRate = 7.50m,
                MaximumAmount = 50000m,
                MaximumTermMonths = 84,
                StandardGrantingFee = 120m,
                VipGrantingFee = 60m,
                IsActive = true,
            },
            new CreditTypeCondition
            {
                CreditType = CreditType.Mortgage,
                Name = "Mortgage",
                StandardAnnualInterestRate = 4.50m,
                VipAnnualInterestRate = 3.90m,
                MaximumAmount = 300000m,
                MaximumTermMonths = 360,
                StandardGrantingFee = 300m,
                VipGrantingFee = 150m,
                IsActive = true,
            },
        };

        var existingConditions = await dbContext.CreditTypeConditions
            .ToListAsync();

        foreach (var seed in seedConditions)
        {
            var existingCondition = existingConditions.FirstOrDefault(condition => condition.CreditType == seed.CreditType);
            if (existingCondition != null)
            {
                continue;
            }

            dbContext.CreditTypeConditions.Add(seed);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(IConfiguration configuration, UserManager<User> userManager)
    {
        var email = configuration["AdminUser:Email"];
        var password = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FirstName = "Bank",
                LastName = "Admin",
                IsActive = true,
                DateCreated = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.User))
        {
            await userManager.AddToRoleAsync(user, RoleNames.User);
        }
    }

}
