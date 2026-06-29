using Bank.Core.JsonModels.Common;
using Bank.DB.Constants;
using Bank.Services.Users.Administration;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Tests.Users;

public class UserServiceGridTests
{
    private static readonly DateTime BaseDateCreated = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private static UserAdministrationService BuildService(IdentityTestHarness harness)
    {
        return new UserAdministrationService(harness.DbContext, harness.UserManager, new FakeUserService());
    }

    // ---------- „Всички потребители“ (служебен грид) ----------

    [Fact]
    public async Task GetRegularUsersAsync_ExcludesAdminAndStaff()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("admin@bank.bg", "Админ", "Админов", BaseDateCreated, roleNames: new[] { RoleNames.Admin });
        await harness.SeedUserAsync("staff@bank.bg", "Служи", "Служев", BaseDateCreated, roleNames: new[] { RoleNames.Staff });
        await harness.SeedUserAsync("user@bank.bg", "Потре", "Потребов", BaseDateCreated, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var result = await service.GetRegularUsersAsync(new PagedRequest { Page = 1, PageSize = 20 }, linked: null, isActive: null);

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Email.Should().Be("user@bank.bg");
    }

    [Fact]
    public async Task GetRegularUsersAsync_PaginatesAndOrdersByDateCreatedDescending()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        for (var index = 0; index < 25; index++)
        {
            await harness.SeedUserAsync($"user{index}@bank.bg", "Потре", index.ToString(), BaseDateCreated.AddMinutes(index), roleNames: new[] { RoleNames.User });
        }
        var service = BuildService(harness);

        var firstPage = await service.GetRegularUsersAsync(new PagedRequest { Page = 1, PageSize = 20 }, linked: null, isActive: null);
        var secondPage = await service.GetRegularUsersAsync(new PagedRequest { Page = 2, PageSize = 20 }, linked: null, isActive: null);

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(25);
        firstPage.Items.Should().HaveCount(20);
        secondPage.Items.Should().HaveCount(5);
        firstPage.Items.Select(i => i.Id).Should().NotIntersectWith(secondPage.Items.Select(i => i.Id));
        // Най-новият (последно засят) трябва да е пръв.
        firstPage.Items.First().Email.Should().Be("user24@bank.bg");
    }

    [Fact]
    public async Task GetRegularUsersAsync_SearchMatchesEmailNameAndPersonName()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("ivan@bank.bg", "Иван", "Петров", BaseDateCreated, roleNames: new[] { RoleNames.User });
        await harness.SeedUserAsync("maria@bank.bg", "Мария", "Георгиева", BaseDateCreated, roleNames: new[] { RoleNames.User });
        await harness.SeedUserAsync(
            "linked@bank.bg", "Друг", "Акаунт", BaseDateCreated,
            linkPerson: true, personFirstName: "Стоян", personLastName: "Стоянов", roleNames: new[] { RoleNames.User, RoleNames.Customer });
        var service = BuildService(harness);

        var byEmail = await service.GetRegularUsersAsync(new PagedRequest { Search = "maria@" }, linked: null, isActive: null);
        var byName = await service.GetRegularUsersAsync(new PagedRequest { Search = "Петров" }, linked: null, isActive: null);
        var byPersonName = await service.GetRegularUsersAsync(new PagedRequest { Search = "Стоян Стоянов" }, linked: null, isActive: null);
        var caseInsensitive = await service.GetRegularUsersAsync(new PagedRequest { Search = "ПЕТРОВ" }, linked: null, isActive: null);

        using var _ = new AssertionScope();
        byEmail.Items.Should().ContainSingle().Which.Email.Should().Be("maria@bank.bg");
        byName.Items.Should().ContainSingle().Which.Email.Should().Be("ivan@bank.bg");
        byPersonName.Items.Should().ContainSingle().Which.Email.Should().Be("linked@bank.bg");
        caseInsensitive.Items.Should().ContainSingle().Which.Email.Should().Be("ivan@bank.bg");
    }

    [Fact]
    public async Task GetRegularUsersAsync_FiltersByLinkedAndStatus()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("linked-active@bank.bg", "А", "А", BaseDateCreated, isActive: true, linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        await harness.SeedUserAsync("unlinked-active@bank.bg", "Б", "Б", BaseDateCreated, isActive: true, roleNames: new[] { RoleNames.User });
        await harness.SeedUserAsync("unlinked-inactive@bank.bg", "В", "В", BaseDateCreated, isActive: false, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var linked = await service.GetRegularUsersAsync(new PagedRequest(), linked: true, isActive: null);
        var unlinked = await service.GetRegularUsersAsync(new PagedRequest(), linked: false, isActive: null);
        var inactive = await service.GetRegularUsersAsync(new PagedRequest(), linked: null, isActive: false);

        using var _ = new AssertionScope();
        linked.Items.Should().ContainSingle().Which.Email.Should().Be("linked-active@bank.bg");
        unlinked.Items.Select(i => i.Email).Should().BeEquivalentTo("unlinked-active@bank.bg", "unlinked-inactive@bank.bg");
        inactive.Items.Should().ContainSingle().Which.Email.Should().Be("unlinked-inactive@bank.bg");
    }

    [Fact]
    public async Task GetRegularUsersAsync_SummaryIgnoresSearchAndFilters()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("admin@bank.bg", "Админ", "Админов", BaseDateCreated, roleNames: new[] { RoleNames.Admin });
        await harness.SeedUserAsync("linked-active@bank.bg", "А", "А", BaseDateCreated, isActive: true, linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        await harness.SeedUserAsync("unlinked-active@bank.bg", "Б", "Б", BaseDateCreated, isActive: true, roleNames: new[] { RoleNames.User });
        await harness.SeedUserAsync("unlinked-inactive@bank.bg", "В", "В", BaseDateCreated, isActive: false, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        // Тесен филтър — обобщението трябва да отразява целия базов набор от обикновени потребители (без Admin).
        var result = await service.GetRegularUsersAsync(new PagedRequest { Search = "linked-active" }, linked: true, isActive: true);

        using var _ = new AssertionScope();
        result.Items.Should().ContainSingle();
        result.Summary.Total.Should().Be(3, "администраторът е извън базата на обикновените потребители");
        result.Summary.Linked.Should().Be(1);
        result.Summary.MissingCustomer.Should().Be(2);
        result.Summary.Active.Should().Be(2);
        result.Summary.Inactive.Should().Be(1);
    }

    [Fact]
    public async Task GetRegularUsersAsync_MapsRolesFromNavigation()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("linked@bank.bg", "А", "А", BaseDateCreated, linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        var service = BuildService(harness);

        var result = await service.GetRegularUsersAsync(new PagedRequest(), linked: null, isActive: null);

        result.Items.Single().Roles.Should().BeEquivalentTo(new[] { UserRoleModel.User, UserRoleModel.Customer });
    }

    [Fact]
    public async Task GetRegularUsersAsync_ClampsPageSizeAndPage()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        for (var index = 0; index < 3; index++)
        {
            await harness.SeedUserAsync($"user{index}@bank.bg", "П", index.ToString(), BaseDateCreated.AddMinutes(index), roleNames: new[] { RoleNames.User });
        }
        var service = BuildService(harness);

        var clampedSize = await service.GetRegularUsersAsync(new PagedRequest { Page = 1, PageSize = 500 }, linked: null, isActive: null);
        var beyondRange = await service.GetRegularUsersAsync(new PagedRequest { Page = int.MaxValue, PageSize = 20 }, linked: null, isActive: null);

        using var _ = new AssertionScope();
        clampedSize.PageSize.Should().Be(100);
        clampedSize.Items.Should().HaveCount(3);
        beyondRange.Page.Should().Be(1);
        beyondRange.Items.Should().HaveCount(3);
    }

    // ---------- Административен грид с достъп ----------

    [Fact]
    public async Task GetUsersForAdministrationAsync_PaginatesAllUsers()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("admin@bank.bg", "Админ", "Админов", BaseDateCreated.AddMinutes(3), roleNames: new[] { RoleNames.Admin });
        await harness.SeedUserAsync("staff@bank.bg", "Служи", "Служев", BaseDateCreated.AddMinutes(2), roleNames: new[] { RoleNames.Staff });
        await harness.SeedUserAsync("customer@bank.bg", "Клие", "Клиев", BaseDateCreated.AddMinutes(1), linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        await harness.SeedUserAsync("user@bank.bg", "Потре", "Потребов", BaseDateCreated, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var firstPage = await service.GetUsersForAdministrationAsync(new PagedRequest { Page = 1, PageSize = 2 }, roles: Array.Empty<UserRoleModel>(), isActive: null);
        var secondPage = await service.GetUsersForAdministrationAsync(new PagedRequest { Page = 2, PageSize = 2 }, roles: Array.Empty<UserRoleModel>(), isActive: null);

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(4);
        firstPage.Items.Should().HaveCount(2);
        secondPage.Items.Should().HaveCount(2);
        // Подреждане по DateCreated desc: admin (newest) е пръв.
        firstPage.Items.First().Email.Should().Be("admin@bank.bg");
        firstPage.Items.Select(i => i.Id).Should().NotIntersectWith(secondPage.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUsersForAdministrationAsync_RoleFacetFilterIsOrWithinFacet()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("admin@bank.bg", "Админ", "Админов", BaseDateCreated, roleNames: new[] { RoleNames.Admin });
        await harness.SeedUserAsync("staff@bank.bg", "Служи", "Служев", BaseDateCreated, roleNames: new[] { RoleNames.Staff });
        await harness.SeedUserAsync("customer@bank.bg", "Клие", "Клиев", BaseDateCreated, linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        await harness.SeedUserAsync("user@bank.bg", "Потре", "Потребов", BaseDateCreated, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var adminsOnly = await service.GetUsersForAdministrationAsync(new PagedRequest(), roles: new[] { UserRoleModel.Admin }, isActive: null);
        var adminOrStaff = await service.GetUsersForAdministrationAsync(new PagedRequest(), roles: new[] { UserRoleModel.Admin, UserRoleModel.Staff }, isActive: null);
        var customers = await service.GetUsersForAdministrationAsync(new PagedRequest(), roles: new[] { UserRoleModel.Customer }, isActive: null);

        using var _ = new AssertionScope();
        adminsOnly.Items.Should().ContainSingle().Which.Email.Should().Be("admin@bank.bg");
        adminOrStaff.Items.Select(i => i.Email).Should().BeEquivalentTo("admin@bank.bg", "staff@bank.bg");
        customers.Items.Should().ContainSingle().Which.Email.Should().Be("customer@bank.bg");
    }

    [Fact]
    public async Task GetUsersForAdministrationAsync_SearchAndStatusFilter()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("ivan@bank.bg", "Иван", "Петров", BaseDateCreated, isActive: true, roleNames: new[] { RoleNames.User });
        await harness.SeedUserAsync("maria@bank.bg", "Мария", "Георгиева", BaseDateCreated, isActive: false, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var bySearch = await service.GetUsersForAdministrationAsync(new PagedRequest { Search = "георгиева" }, roles: Array.Empty<UserRoleModel>(), isActive: null);
        var inactiveOnly = await service.GetUsersForAdministrationAsync(new PagedRequest(), roles: Array.Empty<UserRoleModel>(), isActive: false);

        using var _ = new AssertionScope();
        bySearch.Items.Should().ContainSingle().Which.Email.Should().Be("maria@bank.bg");
        inactiveOnly.Items.Should().ContainSingle().Which.Email.Should().Be("maria@bank.bg");
    }

    [Fact]
    public async Task GetUsersForAdministrationAsync_SummaryCountsAllUsers()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("admin@bank.bg", "Админ", "Админов", BaseDateCreated, isActive: true, roleNames: new[] { RoleNames.Admin });
        await harness.SeedUserAsync("staff@bank.bg", "Служи", "Служев", BaseDateCreated, isActive: true, roleNames: new[] { RoleNames.Staff });
        await harness.SeedUserAsync("customer@bank.bg", "Клие", "Клиев", BaseDateCreated, isActive: true, linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        await harness.SeedUserAsync("user@bank.bg", "Потре", "Потребов", BaseDateCreated, isActive: false, roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        // Филтриране до администраторите — обобщението пак трябва да брои всички потребители.
        var result = await service.GetUsersForAdministrationAsync(new PagedRequest(), roles: new[] { UserRoleModel.Admin }, isActive: null);

        using var _ = new AssertionScope();
        result.Items.Should().ContainSingle();
        result.Summary.TotalUsers.Should().Be(4);
        result.Summary.Admins.Should().Be(1);
        result.Summary.Staff.Should().Be(1);
        result.Summary.Customers.Should().Be(1);
        result.Summary.Active.Should().Be(3);
        result.Summary.Inactive.Should().Be(1);
    }

    [Fact]
    public async Task GetUsersForAdministrationAsync_MapsRolesFromNavigation()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        await harness.SeedUserAsync("customer@bank.bg", "Клие", "Клиев", BaseDateCreated, linkPerson: true, roleNames: new[] { RoleNames.User, RoleNames.Customer });
        var service = BuildService(harness);

        var result = await service.GetUsersForAdministrationAsync(new PagedRequest(), roles: Array.Empty<UserRoleModel>(), isActive: null);

        result.Items.Single().Roles.Should().BeEquivalentTo(new[] { UserRoleModel.User, UserRoleModel.Customer });
    }

    [Fact]
    public async Task GetUsersForAdministrationAsync_ClampsPageBeyondRange()
    {
        await using var harness = IdentityTestHarness.Create(Guid.NewGuid().ToString("N"));
        for (var index = 0; index < 3; index++)
        {
            await harness.SeedUserAsync($"user{index}@bank.bg", "П", index.ToString(), BaseDateCreated.AddMinutes(index), roleNames: new[] { RoleNames.User });
        }
        var service = BuildService(harness);

        var result = await service.GetUsersForAdministrationAsync(new PagedRequest { Page = int.MaxValue, PageSize = 20 }, roles: Array.Empty<UserRoleModel>(), isActive: null);

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }
}
