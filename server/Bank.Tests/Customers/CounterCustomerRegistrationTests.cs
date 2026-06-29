using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.DB.Constants;
using Bank.Services.Calculators;
using Bank.Services.Credits;
using Bank.Services.Customers;
using Bank.Services.Users.Administration;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.Customers;

public class CounterCustomerRegistrationTests
{
    private const string ValidEgn = "9001010000";
    private const string ValidEik = "831650349";

    private static CustomerService BuildService(IdentityTestHarness harness)
    {
        var userService = new FakeUserService(userId: 1);
        var admin = new UserAdministrationService(harness.DbContext, harness.UserManager, userService);
        var repricing = new CreditRepricingService(harness.DbContext, userService, new CreditCalculatorService(TimeProvider.System));
        return new CustomerService(harness.DbContext, userService, admin, repricing);
    }

    [Fact]
    public async Task RegisterCounterCustomerAsync_AsIndividual_CreatesLinkedCustomerAndCounterUser()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        await harness.EnsureRolesAsync(RoleNames.User, RoleNames.Customer);
        var service = BuildService(harness);

        var result = await service.RegisterCounterCustomerAsync(new RegisterCounterCustomerRequest
        {
            Email = "ivan@bank.bg",
            CustomerType = CustomerType.Individual,
            FirstName = "Иван",
            LastName = "Петров",
            Egn = ValidEgn,
        });

        using var _ = new AssertionScope();
        result.CustomerType.Should().Be(CustomerType.Individual);

        var person = await harness.DbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);
        var user = await harness.UserManager.FindByEmailAsync("ivan@bank.bg");
        user.Should().NotBeNull();
        user!.PersonId.Should().Be(person.Id);
        user.MustChangePassword.Should().BeTrue();
        (await harness.UserManager.CheckPasswordAsync(user, ValidEgn)).Should().BeTrue();
        (await harness.UserManager.IsInRoleAsync(user, RoleNames.Customer)).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCounterCustomerAsync_AsCompany_CreatesCompanyWithSingleRepresentativeAndLinksLogin()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        await harness.EnsureRolesAsync(RoleNames.User, RoleNames.Customer);
        var service = BuildService(harness);

        var result = await service.RegisterCounterCustomerAsync(new RegisterCounterCustomerRequest
        {
            Email = "rep@acme.bg",
            CustomerType = CustomerType.Company,
            FirstName = "Никола",
            LastName = "Петков",
            Egn = ValidEgn,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            RepresentativeRole = RepresentativeRole.Manager,
        });

        using var _ = new AssertionScope();
        result.CustomerType.Should().Be(CustomerType.Company);

        var company = await harness.DbContext.Companies.Include(c => c.Representatives).AsNoTracking().FirstAsync(c => c.Eik == ValidEik);
        company.Representatives.Should().ContainSingle();

        var person = await harness.DbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);
        var user = await harness.UserManager.FindByEmailAsync("rep@acme.bg");
        user!.PersonId.Should().Be(person.Id);
        user.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCounterCustomerAsync_WithExistingEmail_Throws()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        await harness.EnsureRolesAsync(RoleNames.User, RoleNames.Customer);
        await harness.SeedUserAsync("taken@bank.bg", "Стар", "Потребител", new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc), roleNames: new[] { RoleNames.User });
        var service = BuildService(harness);

        var act = () => service.RegisterCounterCustomerAsync(new RegisterCounterCustomerRequest
        {
            Email = "taken@bank.bg",
            CustomerType = CustomerType.Individual,
            FirstName = "Иван",
            LastName = "Петров",
            Egn = ValidEgn,
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*имейл*");
    }

    [Fact]
    public async Task RegisterCounterCustomerAsync_WithInvalidEgn_ThrowsAndCreatesNoUser()
    {
        await using var harness = IdentityTestHarness.Create(System.Guid.NewGuid().ToString("N"));
        await harness.EnsureRolesAsync(RoleNames.User, RoleNames.Customer);
        var service = BuildService(harness);

        var act = () => service.RegisterCounterCustomerAsync(new RegisterCounterCustomerRequest
        {
            Email = "bad@bank.bg",
            CustomerType = CustomerType.Individual,
            FirstName = "Иван",
            LastName = "Петров",
            Egn = "9001010001", // невалидна контролна цифра
        });

        using var _ = new AssertionScope();
        (await act.Should().ThrowAsync<BankException>()).WithMessage("*валиден ЕГН*");
        // Валидацията е преди създаването на акаунта -> няма orphan потребител (важи и при InMemory).
        (await harness.UserManager.FindByEmailAsync("bad@bank.bg")).Should().BeNull();
    }
}
