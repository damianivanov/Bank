using Bank.Core.Exceptions;
using Bank.DB.Constants;
using Bank.Services.Users.Administration;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Bank.Tests.Users;

public class CounterUserCreationTests
{
    private const string Egn = "9001010000";

    private static UserAdministrationService BuildService(IdentityTestHarness harness)
        => new(harness.DbContext, harness.UserManager, new FakeUserService(userId: 1));

    [Fact]
    public async Task CreateCounterUserAsync_CreatesActiveUserWithUserRoleFlagAndEgnPassword()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        await harness.EnsureRolesAsync(RoleNames.User);
        var service = BuildService(harness);

        var userId = await service.CreateCounterUserAsync("walkin@bank.bg", Egn, mustChangePassword: true);

        var user = await harness.UserManager.FindByIdAsync(userId.ToString());
        using var _ = new AssertionScope();
        user.Should().NotBeNull();
        user!.Email.Should().Be("walkin@bank.bg");
        user.UserName.Should().Be("walkin@bank.bg");
        user.IsActive.Should().BeTrue();
        user.MustChangePassword.Should().BeTrue();
        (await harness.UserManager.IsInRoleAsync(user, RoleNames.User)).Should().BeTrue();
        (await harness.UserManager.CheckPasswordAsync(user, Egn)).Should().BeTrue("паролата трябва да е ЕГН-то");
    }

    [Fact]
    public async Task CreateCounterUserAsync_WithExistingEmail_Throws()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        await harness.EnsureRolesAsync(RoleNames.User);
        await harness.SeedUserAsync("taken@bank.bg", "Стар", "Потребител", new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc), roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var act = () => service.CreateCounterUserAsync("taken@bank.bg", Egn, mustChangePassword: true);

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*имейл*");
    }
}
