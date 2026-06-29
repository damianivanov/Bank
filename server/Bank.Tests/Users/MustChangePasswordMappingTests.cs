using Bank.DB.Constants;
using Bank.Services.Users;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Bank.Tests.Users;

public class MustChangePasswordMappingTests
{
    private static readonly System.DateTime BaseDateCreated = new(2026, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentUserAsync_SurfacesMustChangePasswordFlag()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        var user = await harness.SeedUserAsync("counter@bank.bg", "Иван", "Петров", BaseDateCreated, roleNames: new[] { RoleNames.User });
        user.MustChangePassword = true;
        await harness.UserManager.UpdateAsync(user);

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = harness.BuildPrincipal(user, isAdmin: false) },
        };
        var userService = new UserService(accessor, harness.DbContext, harness.UserManager);

        var result = await userService.GetCurrentUserAsync();

        result.Should().NotBeNull();
        result!.MustChangePassword.Should().BeTrue();
    }
}
