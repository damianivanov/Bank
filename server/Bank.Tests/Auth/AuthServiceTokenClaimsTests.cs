using Bank.Core.JsonModels.Auth;
using Bank.Core.Settings;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Auth;
using Bank.Services.Users;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bank.Tests.Auth;

public class AuthServiceTokenClaimsTests
{
    private const string Password = "9001010000";
    private static readonly System.DateTime BaseDateCreated = new(2026, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);

    private static AuthService BuildAuthService(IdentityTestHarness harness, User user)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-jwt-signing-key-at-least-32-bytes-long-1234567890",
            })
            .Build();
        var settings = new ApplicationSettings(configuration);
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = harness.BuildPrincipal(user, isAdmin: false) },
        };
        var userService = new UserService(accessor, harness.DbContext, harness.UserManager);
        return new AuthService(harness.DbContext, harness.UserManager, userService, settings);
    }

    private static async Task<User> SeedUserAsync(IdentityTestHarness harness)
    {
        await harness.EnsureRolesAsync(RoleNames.User);
        var user = new User
        {
            UserName = "operator@bank.bg",
            Email = "operator@bank.bg",
            IsActive = true,
            DateCreated = BaseDateCreated,
        };
        var createResult = await harness.UserManager.CreateAsync(user, Password);
        if (!createResult.Succeeded)
        {
            throw new System.InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }
        await harness.UserManager.AddToRoleAsync(user, RoleNames.User);
        return user;
    }

    [Fact]
    public async Task LoginAsync_IssuesTokenWhoseIdentityNameResolvesToUserName()
    {
        // Логването на грешки записва context.HttpContext.User.Identity?.Name. Ако токенът не носи
        // име-клейм, Identity.Name е null и колоната UserName остава празна за всяка грешка.
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        var user = await SeedUserAsync(harness);
        var service = BuildAuthService(harness, user);

        var result = await service.LoginAsync(new LoginRequest { Email = user.Email!, Password = Password });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        // Реконструираме принципала както го прави JwtBearer: NameClaimType по подразбиране е ClaimTypes.Name.
        var identity = new ClaimsIdentity(jwt.Claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);

        identity.Name.Should().Be(user.UserName);
    }
}
