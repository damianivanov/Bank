using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Common;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Common;
using Bank.Services.Users;
using Bank.Services.Users.Administration;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.Users;

// Инвариант: името на потребител се пази на едно място. Свързан с лице акаунт черпи името от
// лицето (единствен източник, KYC); несвързан акаунт (служител, още нерегистриран клиент) — от себе си.
public class UserNameSourceOfTruthTests
{
    private static readonly DateTime BaseDateCreated = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private static UserAdministrationService BuildAdminService(IdentityTestHarness harness)
    {
        return new UserAdministrationService(harness.DbContext, harness.UserManager, new FakeUserService());
    }

    private static UserService BuildUserService(IdentityTestHarness harness, User user)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = harness.BuildPrincipal(user, isAdmin: false) },
        };

        return new UserService(accessor, harness.DbContext, harness.UserManager);
    }

    // ---------- Резолвер на име ----------

    [Fact]
    public void Resolve_LinkedUser_ReturnsPersonName()
    {
        var user = new User
        {
            FirstName = "Остаряло",
            LastName = "Копие",
            PersonId = 5,
            Person = new Person { FirstName = "Иван", LastName = "Петров" },
        };

        var (firstName, lastName) = UserNameResolver.Resolve(user);

        using var _ = new AssertionScope();
        firstName.Should().Be("Иван");
        lastName.Should().Be("Петров");
    }

    [Fact]
    public void Resolve_UnlinkedUser_ReturnsAccountName()
    {
        var user = new User { FirstName = "Служи", LastName = "Служев", PersonId = null, Person = null };

        var (firstName, lastName) = UserNameResolver.Resolve(user);

        using var _ = new AssertionScope();
        firstName.Should().Be("Служи");
        lastName.Should().Be("Служев");
    }

    // ---------- Свързване нулира копието на акаунта ----------

    [Fact]
    public async Task LinkUserToPersonAsync_ClearsAccountNameAndReturnsPersonName()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        var user = await harness.SeedUserAsync("user@bank.bg", "Иван", "Петров", BaseDateCreated, roleNames: new[] { RoleNames.User });

        var person = new Person { FirstName = "Реално", LastName = "Лице", Egn = "0011223344" };
        harness.DbContext.Persons.Add(person);
        await harness.DbContext.SaveChangesAsync();

        var service = BuildAdminService(harness);

        var result = await service.LinkUserToPersonAsync(user.Id, person.Id);

        var stored = await harness.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

        using var _ = new AssertionScope();
        // Копието на акаунта се изчиства — името вече живее само на лицето.
        stored.FirstName.Should().BeNull();
        stored.LastName.Should().BeNull();
        stored.PersonId.Should().Be(person.Id);
        // Върнатият модел показва името на лицето.
        result.FirstName.Should().Be("Реално");
        result.LastName.Should().Be("Лице");
    }

    // ---------- Гридовете показват името на лицето за свързан акаунт ----------

    [Fact]
    public async Task GetRegularUsersAsync_LinkedUser_ShowsPersonNameNotStaleAccountName()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        // Симулираме заварен запис, който още носи остаряло копие на името върху акаунта.
        await harness.SeedUserAsync(
            "linked@bank.bg", "Старо", "Копие", BaseDateCreated,
            linkPerson: true, personFirstName: "Реално", personLastName: "Лице",
            roleNames: new[] { RoleNames.User, RoleNames.Customer });
        var service = BuildAdminService(harness);

        var result = await service.GetRegularUsersAsync(new PagedRequest(), linked: true, isActive: null);

        var item = result.Items.Should().ContainSingle().Subject;
        using var _ = new AssertionScope();
        item.FirstName.Should().Be("Реално");
        item.LastName.Should().Be("Лице");
    }

    // ---------- Профил: свързан клиент не може да си сменя името (KYC) ----------

    [Fact]
    public async Task UpdateProfileAsync_LinkedUser_DoesNotChangeName()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        var user = await harness.SeedUserAsync(
            "linked@bank.bg", firstName: null, lastName: null, BaseDateCreated,
            linkPerson: true, personFirstName: "Иван", personLastName: "Петров",
            roleNames: new[] { RoleNames.User, RoleNames.Customer });
        var service = BuildUserService(harness, user);

        var result = await service.UpdateProfileAsync(new UpdateProfileRequest { FirstName = "Нов", LastName = "Опит" });

        var storedUser = await harness.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        var storedPerson = await harness.DbContext.Persons.AsNoTracking().SingleAsync(p => p.Id == user.PersonId);

        using var _ = new AssertionScope();
        // Връща меродавното име на лицето, а не подаденото.
        result.FirstName.Should().Be("Иван");
        result.LastName.Should().Be("Петров");
        // Нито акаунтът, нито лицето се променят.
        storedUser.FirstName.Should().BeNull();
        storedUser.LastName.Should().BeNull();
        storedPerson.FirstName.Should().Be("Иван");
        storedPerson.LastName.Should().Be("Петров");
    }

    [Fact]
    public async Task UpdateProfileAsync_UnlinkedUser_UpdatesAccountName()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        var user = await harness.SeedUserAsync("staff@bank.bg", "Стар", "Име", BaseDateCreated, roleNames: new[] { RoleNames.User });
        var service = BuildUserService(harness, user);

        var result = await service.UpdateProfileAsync(new UpdateProfileRequest { FirstName = "Нов", LastName = "Име" });

        var storedUser = await harness.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

        using var _ = new AssertionScope();
        result.FirstName.Should().Be("Нов");
        result.LastName.Should().Be("Име");
        storedUser.FirstName.Should().Be("Нов");
        storedUser.LastName.Should().Be("Име");
    }

    // ---------- Текущ потребител: свързан акаунт връща името на лицето ----------

    [Fact]
    public async Task GetCurrentUserAsync_LinkedUser_ReturnsPersonName()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        var user = await harness.SeedUserAsync(
            "linked@bank.bg", firstName: null, lastName: null, BaseDateCreated,
            linkPerson: true, personFirstName: "Мария", personLastName: "Иванова",
            roleNames: new[] { RoleNames.User, RoleNames.Customer });
        var service = BuildUserService(harness, user);

        var result = await service.GetCurrentUserAsync();

        result.Should().NotBeNull();
        using var _ = new AssertionScope();
        result!.FirstName.Should().Be("Мария");
        result.LastName.Should().Be("Иванова");
    }
}
