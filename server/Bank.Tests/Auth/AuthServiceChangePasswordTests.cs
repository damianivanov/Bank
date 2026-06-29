using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.Core.Settings;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Auth;
using Bank.Services.Users;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bank.Tests.Auth;

public class AuthServiceChangePasswordTests
{
    private const string InitialPassword = "9001010000";
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

    private static async Task<User> SeedCounterUserAsync(IdentityTestHarness harness)
    {
        await harness.EnsureRolesAsync(RoleNames.User, RoleNames.Customer);
        var user = new User
        {
            UserName = "counter@bank.bg",
            Email = "counter@bank.bg",
            IsActive = true,
            DateCreated = BaseDateCreated,
            MustChangePassword = true,
        };
        var createResult = await harness.UserManager.CreateAsync(user, InitialPassword);
        if (!createResult.Succeeded)
        {
            throw new System.InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }
        await harness.UserManager.AddToRoleAsync(user, RoleNames.User);
        return user;
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_ChangesPasswordAndClearsFlag()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        var user = await SeedCounterUserAsync(harness);
        var service = BuildAuthService(harness, user);

        var result = await service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = InitialPassword,
            NewPassword = "NovaParola1",
        });

        var stored = await harness.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        var reloaded = await harness.UserManager.FindByIdAsync(user.Id.ToString());

        using var _ = new AssertionScope();
        result.AccessToken.Should().NotBeNullOrWhiteSpace("смяната преиздава нова сесия");
        stored.MustChangePassword.Should().BeFalse();
        (await harness.UserManager.CheckPasswordAsync(reloaded!, "NovaParola1")).Should().BeTrue();
        (await harness.UserManager.CheckPasswordAsync(reloaded!, InitialPassword)).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsAndKeepsFlag()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        var user = await SeedCounterUserAsync(harness);
        var service = BuildAuthService(harness, user);

        var act = () => service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "GreshnaParola1",
            NewPassword = "NovaParola1",
        });

        await act.Should().ThrowAsync<BankException>();

        var stored = await harness.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        stored.MustChangePassword.Should().BeTrue("неуспешната смяна не маха флага");
    }
}
