using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Credits;
using Bank.Services.Customers;
using Bank.Services.Users;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.Customers;

public class CustomerServiceTests
{
    private const string ValidEgn = "9001010000";
    private const string AnotherValidEgn = "0142011239";
    private const string ValidEik = "831650349";

    private static CustomerService BuildService(AppDbContext dbContext)
    {
        IUserService userService = new FakeUserService();
        var repricing = new CreditRepricingService(dbContext, userService, new CreditCalculatorService(TimeProvider.System));
        return new CustomerService(dbContext, userService, new FakeUserAdministrationService(), repricing);
    }

    [Fact]
    public async Task CreateCustomerAsync_AsIndividual_PersistsIndividualFields()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var result = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = ValidEgn,
        });

        var details = await service.GetCustomerForEditAsync(result.Id);

        using var _ = new AssertionScope();
        result.CustomerType.Should().Be(CustomerType.Individual);
        details.FirstName.Should().Be("Georgi");
        details.LastName.Should().Be("Stoyanov");
        details.PersonalIdentifier.Should().Be(ValidEgn);
        details.CompanyName.Should().BeNull();
    }

    [Fact]
    public async Task CreateCustomerAsync_AsCompany_PersistsCompanyFieldsAndRepresentative()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var result = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = "Nikola",
                    LastName = "Petkov",
                    Egn = ValidEgn,
                    Role = RepresentativeRole.Manager,
                },
            ],
        });

        var details = await service.GetCustomerForEditAsync(result.Id);

        using var _ = new AssertionScope();
        result.CustomerType.Should().Be(CustomerType.Company);
        details.CompanyName.Should().Be("Acme OOD");
        details.CompanyIdentifier.Should().Be(ValidEik);
        details.PersonalIdentifier.Should().BeNull();
        details.Representatives.Should().ContainSingle();
        details.Representatives.Single().FirstName.Should().Be("Nikola");
        details.Representatives.Single().Egn.Should().Be(ValidEgn);
        details.Representatives.Single().Role.Should().Be(RepresentativeRole.Manager);
    }

    [Fact]
    public async Task CreateCustomerAsync_ReturnsLightSummary_WithDisplayNameAndIdentifier()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        // Създаването връща леко резюме (без сметки/кредити); детайлите се теглят отделно при нужда.
        var result = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = ValidEgn,
        });

        using var _ = new AssertionScope();
        result.Id.Should().BeGreaterThan(0);
        result.CustomerType.Should().Be(CustomerType.Individual);
        result.DisplayName.Should().NotBeNullOrWhiteSpace();
        result.Identifier.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetCustomerForEditAsync_ReturnsIdentityAndRepresentatives()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var created = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        var edit = await service.GetCustomerForEditAsync(created.Id);

        using var _ = new AssertionScope();
        edit.Id.Should().Be(created.Id);
        edit.CustomerType.Should().Be(CustomerType.Company);
        edit.CompanyName.Should().Be("Acme OOD");
        edit.CompanyIdentifier.Should().Be(ValidEik);
        edit.Representatives.Should().ContainSingle().Which.FirstName.Should().Be("Nikola");
    }

    [Fact]
    public async Task GetCustomerForEditAsync_WhenMissing_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.GetCustomerForEditAsync(999);

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*не е намерен*");
    }

    [Fact]
    public async Task CreateCustomerAsync_AsCompanyWithoutRepresentatives_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*поне един представител*");
    }

    [Fact]
    public async Task CreateCustomerAsync_WhenRepresentativeAndIndividualShareEgn_ReusesOnePerson()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var company = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = "Nikola",
                    LastName = "Petkov",
                    Egn = ValidEgn,
                    Role = RepresentativeRole.Manager,
                },
            ],
        });

        var individual = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Nikola",
            LastName = "Petkov",
            PersonalIdentifier = ValidEgn,
        });

        using var _ = new AssertionScope();
        var persons = await dbContext.Persons.AsNoTracking().Where(p => p.Egn == ValidEgn).ToListAsync();
        persons.Should().ContainSingle("the representative and the individual customer are one person");

        var personId = persons.Single().Id;
        var companyDetails = await service.GetCustomerForEditAsync(company.Id);
        companyDetails.Representatives.Single().PersonId.Should().Be(personId);

        var individualCustomer = await dbContext.Customers.AsNoTracking().FirstAsync(c => c.Id == individual.Id);
        individualCustomer.PersonId.Should().Be(personId);
    }

    [Fact]
    public async Task CreateCustomerAsync_AsIndividualCarryingCompanyFields_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = ValidEgn,
            CompanyName = "Should not be here",
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*Полетата за фирма трябва да са празни*");
    }

    [Fact]
    public async Task CreateCustomerAsync_WithInvalidEgn_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = "9001010001",
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*валиден ЕГН*");
    }

    [Fact]
    public async Task CreateCustomerAsync_WithDuplicatePersonalIdentifier_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = ValidEgn,
        });

        var act = () => service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Different",
            LastName = "Person",
            PersonalIdentifier = ValidEgn,
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*съществува клиент*");
    }

    [Fact]
    public async Task CreateCustomerAsync_WhenSameRepresentativeListedUnderTwoRoles_ReusesOnePerson()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var company = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Owner },
            ],
        });

        using var _ = new AssertionScope();
        var persons = await dbContext.Persons.AsNoTracking().Where(p => p.Egn == ValidEgn).ToListAsync();
        persons.Should().ContainSingle();
        var companyDetails = await service.GetCustomerForEditAsync(company.Id);
        companyDetails.Representatives.Should().HaveCount(2);
        companyDetails.Representatives.Select(r => r.PersonId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task GetAccessibleCustomerIdsAsync_ExcludesCustomerWhenRepresentationHasExpired()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = "Nikola",
                    LastName = "Petkov",
                    Egn = ValidEgn,
                    Role = RepresentativeRole.Manager,
                    ValidFrom = new DateOnly(2019, 1, 1),
                    ValidTo = new DateOnly(2020, 1, 1),
                },
            ],
        });

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);

        var accessible = await service.GetAccessibleCustomerIdsAsync(person.Id);

        accessible.Should().BeEmpty("the representative's mandate expired in 2020");
    }

    [Fact]
    public async Task GetAccessibleCustomerIdsAsync_IncludesOwnCustomerAndActivelyRepresentedCompany()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Nikola",
            LastName = "Petkov",
            PersonalIdentifier = ValidEgn,
        });

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);

        var accessible = await service.GetAccessibleCustomerIdsAsync(person.Id);

        accessible.Should().HaveCount(2, "the person is their own individual customer and an active representative of the company");
    }

    [Fact]
    public async Task GetAccessibleCustomersAsync_ReturnsOwnIndividualThenRepresentedCompany_WithDisplayNames()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Nikola",
            LastName = "Petkov",
            PersonalIdentifier = ValidEgn,
        });

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);

        var accessible = await service.GetAccessibleCustomersAsync(person.Id);

        using var _ = new AssertionScope();
        accessible.Should().HaveCount(2, "лицето е собствен клиент-физическо лице и активен представител на фирмата");
        // Собственото физическо лице е първо (за подразбиращ се избор в /my-banking).
        accessible.First().CustomerType.Should().Be(CustomerType.Individual);
        accessible.Last().CustomerType.Should().Be(CustomerType.Company);
        accessible.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.DisplayName));
    }

    [Fact]
    public async Task GetAccessibleCustomersAsync_ExcludesCompanyWhenRepresentationExpired()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = "Nikola",
                    LastName = "Petkov",
                    Egn = ValidEgn,
                    Role = RepresentativeRole.Manager,
                    ValidFrom = new DateOnly(2019, 1, 1),
                    ValidTo = new DateOnly(2020, 1, 1),
                },
            ],
        });

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);

        var accessible = await service.GetAccessibleCustomersAsync(person.Id);

        accessible.Should().BeEmpty("мандатът на представителя е изтекъл през 2020 г.");
    }

    [Fact]
    public async Task UpdateCustomerAsync_AsIndividual_WhenNameChangesWithSameEgn_UpdatesPersonName()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var created = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = ValidEgn,
        });

        // Същото ЕГН (същото лице), но коригирана фамилия — промяната трябва да се запази.
        var updated = await service.UpdateCustomerAsync(created.Id, new UpdateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanovski",
            PersonalIdentifier = ValidEgn,
        });

        var updatedDetails = await service.GetCustomerForEditAsync(updated.Id);

        using var _ = new AssertionScope();
        updatedDetails.FirstName.Should().Be("Georgi");
        updatedDetails.LastName.Should().Be("Stoyanovski");

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);
        person.LastName.Should().Be("Stoyanovski");
    }

    [Fact]
    public async Task UpdateCustomerAsync_WhenRemovingRepresentativeBoundToLogin_Throws()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var dbContext = TestDbContextFactory.CreateContext(databaseName);
        var service = BuildService(dbContext);

        var company = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        // Логин акаунт, свързан с лицето на представителя.
        var representativePerson = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);
        dbContext.Users.Add(new User { UserName = "rep@bank.bg", Email = "rep@bank.bg", PersonId = representativePerson.Id });
        await dbContext.SaveChangesAsync();

        // Подмяната на единствения представител би осиротила свързания акаунт -> трябва да се блокира.
        var act = () => service.UpdateCustomerAsync(company.Id, new UpdateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Petar", LastName = "Ivanov", Egn = AnotherValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*свързан*");

        // Промяната е отхвърлена изцяло — представителят е запазен.
        await using var verifyContext = TestDbContextFactory.CreateContext(databaseName);
        var reps = await verifyContext.CompanyRepresentatives.AsNoTracking().Include(r => r.Person).ToListAsync();
        reps.Should().ContainSingle().Which.Person.Egn.Should().Be(ValidEgn);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WhenRemovingRepresentativeNotBoundToLogin_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var company = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        // Никой акаунт не е свързан с представителя -> подмяната е разрешена.
        await service.UpdateCustomerAsync(company.Id, new UpdateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Petar", LastName = "Ivanov", Egn = AnotherValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        var details = await service.GetCustomerForEditAsync(company.Id);
        details.Representatives.Should().ContainSingle().Which.Egn.Should().Be(AnotherValidEgn);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WhenChangingIndividualPersonBoundToLogin_Throws()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var dbContext = TestDbContextFactory.CreateContext(databaseName);
        var service = BuildService(dbContext);

        var customer = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = ValidEgn,
        });

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);
        dbContext.Users.Add(new User { UserName = "ind@bank.bg", Email = "ind@bank.bg", PersonId = person.Id });
        await dbContext.SaveChangesAsync();

        // Смяна на ЕГН-то означава друго лице -> старото лице осиротява заедно със свързания акаунт.
        var act = () => service.UpdateCustomerAsync(customer.Id, new UpdateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Petar",
            LastName = "Ivanov",
            PersonalIdentifier = AnotherValidEgn,
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*свързан*");
    }

    [Fact]
    public async Task UpdateCustomerAsync_WhenRemovedRepresentativeStillRepresentsAnotherCompany_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        // Едно и също лице е представител на две фирми (общо ЕГН -> едно лице).
        var companyA = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Beta EOOD",
            CompanyIdentifier = "121817309",
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Nikola", LastName = "Petkov", Egn = ValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        var person = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == ValidEgn);
        dbContext.Users.Add(new User { UserName = "rep@bank.bg", Email = "rep@bank.bg", PersonId = person.Id });
        await dbContext.SaveChangesAsync();

        // Премахването на лицето от фирма A е допустимо — то остава представител на фирма B.
        await service.UpdateCustomerAsync(companyA.Id, new UpdateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest { FirstName = "Petar", LastName = "Ivanov", Egn = AnotherValidEgn, Role = RepresentativeRole.Manager },
            ],
        });

        var detailsA = await service.GetCustomerForEditAsync(companyA.Id);
        detailsA.Representatives.Should().ContainSingle().Which.Egn.Should().Be(AnotherValidEgn);
    }

    [Fact]
    public async Task CreateCustomerForUserAsync_WhenOnlyRepresentativeMandateExpired_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        // Единственият представител има изтекъл мандат -> няма активно лице, към което да се закачи логин.
        var act = () => service.CreateCustomerForUserAsync(42, new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = "Nikola",
                    LastName = "Petkov",
                    Egn = ValidEgn,
                    Role = RepresentativeRole.Manager,
                    ValidFrom = new DateOnly(2019, 1, 1),
                    ValidTo = new DateOnly(2020, 1, 1),
                },
            ],
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*активен представител*");
    }

    [Fact]
    public async Task CreateCustomerForUserAsync_WhenOneRepresentativeMandateExpired_LinksToTheActiveRepresentative()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var recording = new RecordingUserAdministrationService();
        var repricing = new CreditRepricingService(dbContext, new FakeUserService(), new CreditCalculatorService(TimeProvider.System));
        var service = new CustomerService(dbContext, new FakeUserService(), recording, repricing);

        await service.CreateCustomerForUserAsync(42, new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = "Acme OOD",
            CompanyIdentifier = ValidEik,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = "Nikola",
                    LastName = "Petkov",
                    Egn = ValidEgn,
                    Role = RepresentativeRole.Manager,
                    ValidFrom = new DateOnly(2019, 1, 1),
                    ValidTo = new DateOnly(2020, 1, 1),
                },
                new CustomerRepresentativeRequest
                {
                    FirstName = "Petar",
                    LastName = "Ivanov",
                    Egn = AnotherValidEgn,
                    Role = RepresentativeRole.Owner,
                },
            ],
        });

        // Акаунтът трябва да се свърже с активния представител, а не с този с изтекъл мандат.
        var activePerson = await dbContext.Persons.AsNoTracking().FirstAsync(p => p.Egn == AnotherValidEgn);
        using var _ = new AssertionScope();
        recording.LastUserId.Should().Be(42);
        recording.LastPersonId.Should().Be(activePerson.Id);
    }

    [Fact]
    public async Task UpdateVipAsync_WhenFlippingToVip_RepricesActiveCreditToVipRate()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        dbContext.CreditTypeConditions.Add(new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Consumer",
            StandardAnnualInterestRate = 8.5m,
            VipAnnualInterestRate = 7.5m,
            MaximumAmount = 50000m,
            MaximumTermMonths = 84,
            StandardGrantingFee = 120m,
            VipGrantingFee = 60m,
            IsActive = true,
        });
        await dbContext.SaveChangesAsync();

        var customer = await service.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Georgi",
            LastName = "Stoyanov",
            PersonalIdentifier = AnotherValidEgn,
        });

        var creditService = new CreditService(dbContext, new FakeUserService(), new CreditCalculatorService(TimeProvider.System), new Bank.Core.Settings.DemoOptions());
        var credit = await creditService.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 10000m,
            TermMonths = 12,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
        });
        credit.AppliedAnnualInterestRate.Should().Be(8.5m, "the customer is not VIP at grant time");

        var updated = await service.UpdateVipAsync(customer.Id, new UpdateCustomerVipRequest { IsVip = true });

        updated.IsVip.Should().BeTrue();
        var repriced = await dbContext.Credits.AsNoTracking().FirstAsync(c => c.Id == credit.Id);
        repriced.AppliedAnnualInterestRate.Should().Be(7.5m);
    }

    private static readonly DateTime BaseDateCreated = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCustomersAsync_ReturnsRequestedPageWithTotalCount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomersAsync(dbContext, 25);
        var service = BuildService(dbContext);

        var firstPage = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20 });
        var secondPage = await service.GetCustomersAsync(new PagedRequest { Page = 2, PageSize = 20 });

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(25);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(20);
        firstPage.Items.Should().HaveCount(20);
        secondPage.Items.Should().HaveCount(5);
        secondPage.Page.Should().Be(2);
        // Страниците не трябва да се припокриват.
        firstPage.Items.Select(customer => customer.Id)
            .Should().NotIntersectWith(secondPage.Items.Select(customer => customer.Id));
    }

    [Fact]
    public async Task GetCustomersAsync_OrdersByDateCreatedDescending()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedIndividualCustomerAsync(dbContext, "Мария", "Георгиева", "0142011239", BaseDateCreated.AddDays(5));
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20 });

        result.Items.First().DisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetCustomersAsync_ClampsExcessivePageSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomersAsync(dbContext, 3);
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 500 });

        using var _ = new AssertionScope();
        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCustomersAsync_NormalizesNonPositivePageAndSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomersAsync(dbContext, 3);
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 0, PageSize = 0 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetCustomersAsync_WithPageBeyondRange_ReturnsLastAvailablePage()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomersAsync(dbContext, 3);
        var service = BuildService(dbContext);

        // Огромна стойност за Page не трябва да препълва int32 при изчисляване на отместването.
        var result = await service.GetCustomersAsync(new PagedRequest { Page = int.MaxValue, PageSize = 20 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCustomersAsync_WithEqualDateCreated_PagesDeterministicallyById()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        for (var index = 1; index <= 4; index++)
        {
            // Еднакво DateCreated за всички — страницирането трябва да разчита на вторичния ключ по Id.
            await SeedIndividualCustomerAsync(dbContext, "Клиент", index.ToString(), $"900101000{index}", BaseDateCreated);
        }
        var service = BuildService(dbContext);

        var firstPage = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 2 });
        var secondPage = await service.GetCustomersAsync(new PagedRequest { Page = 2, PageSize = 2 });

        using var _ = new AssertionScope();
        var ids = firstPage.Items.Concat(secondPage.Items).Select(customer => customer.Id).ToArray();
        ids.Should().HaveCount(4);
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetCustomersAsync_SearchByPersonName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedIndividualCustomerAsync(dbContext, "Мария", "Георгиева", "0142011239", BaseDateCreated);
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Георгиева" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.DisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetCustomersAsync_SearchByFullName_MatchesAcrossNameBoundary()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedIndividualCustomerAsync(dbContext, "Мария", "Георгиева", "0142011239", BaseDateCreated);
        var service = BuildService(dbContext);

        // Търсенето пресича границата между собствено и фамилно име благодарение на конкатенацията.
        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Иван Петров" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.DisplayName.Should().Be("Иван Петров");
    }

    [Fact]
    public async Task GetCustomersAsync_SearchByEgn_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedIndividualCustomerAsync(dbContext, "Мария", "Георгиева", "0142011239", BaseDateCreated);
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "0142011239" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.DisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetCustomersAsync_SearchByCompanyName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCompanyCustomerAsync(dbContext, "Алфа Трейд ООД", "831650349", BaseDateCreated);
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Алфа" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.DisplayName.Should().Be("Алфа Трейд ООД");
    }

    [Fact]
    public async Task GetCustomersAsync_SearchByEik_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCompanyCustomerAsync(dbContext, "Алфа Трейд ООД", "831650349", BaseDateCreated);
        await SeedCompanyCustomerAsync(dbContext, "Бета ЕООД", "121817309", BaseDateCreated);
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "121817309" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.DisplayName.Should().Be("Бета ЕООД");
    }

    [Fact]
    public async Task GetCustomersAsync_SearchIsCaseInsensitive()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Георгиев", "9001010000", BaseDateCreated);
        await SeedCompanyCustomerAsync(dbContext, "Алфа Трейд ООД", "831650349", BaseDateCreated);
        var service = BuildService(dbContext);

        // Различен регистър от записаните данни — пинва нечувствителното към регистъра търсене.
        var byLowerName = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "георгиев" });
        var byLowerCompany = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "алфа" });

        using var _ = new AssertionScope();
        byLowerName.TotalCount.Should().Be(1);
        byLowerCompany.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCustomersAsync_WithCustomerTypeIndividual_ReturnsOnlyIndividuals()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedCompanyCustomerAsync(dbContext, "Алфа ООД", "831650349", BaseDateCreated.AddDays(1));
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20 }, CustomerType.Individual);

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerType.Should().Be(CustomerType.Individual);
    }

    [Fact]
    public async Task GetCustomersAsync_WithCustomerTypeCompany_ReturnsOnlyCompanies()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedCompanyCustomerAsync(dbContext, "Алфа ООД", "831650349", BaseDateCreated.AddDays(1));
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20 }, CustomerType.Company);

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerType.Should().Be(CustomerType.Company);
    }

    [Fact]
    public async Task GetCustomersAsync_WithoutCustomerType_ReturnsBothTypes()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedCompanyCustomerAsync(dbContext, "Алфа ООД", "831650349", BaseDateCreated.AddDays(1));
        var service = BuildService(dbContext);

        var result = await service.GetCustomersAsync(new PagedRequest { Page = 1, PageSize = 20 });

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomerLookupAsync_FiltersBySearch()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualCustomerAsync(dbContext, "Иван", "Петров", "9001010000", BaseDateCreated);
        await SeedIndividualCustomerAsync(dbContext, "Мария", "Георгиева", "0142011239", BaseDateCreated);
        var service = BuildService(dbContext);

        var result = await service.GetCustomerLookupAsync("Георгиева");

        using var _ = new AssertionScope();
        result.Should().ContainSingle().Which.DisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetCustomerLookupAsync_CapsResultsToFifty()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        // Падащото меню е typeahead — без заявка връща най-много 50 записа, не целия списък.
        await SeedIndividualCustomersAsync(dbContext, 60);
        var service = BuildService(dbContext);

        var result = await service.GetCustomerLookupAsync(null);

        result.Should().HaveCount(50);
    }

    private static async Task SeedIndividualCustomersAsync(AppDbContext dbContext, int count)
    {
        for (var index = 0; index < count; index++)
        {
            dbContext.Customers.Add(new Customer
            {
                CustomerType = CustomerType.Individual,
                DateCreated = BaseDateCreated.AddMinutes(index),
                Person = new Person
                {
                    FirstName = "Клиент",
                    LastName = index.ToString(),
                    Egn = Guid.NewGuid().ToString("N")[..10],
                },
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Customer> SeedIndividualCustomerAsync(
        AppDbContext dbContext, string firstName, string lastName, string egn, DateTime dateCreated)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            DateCreated = dateCreated,
            Person = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                Egn = egn,
            },
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    private static async Task<Customer> SeedCompanyCustomerAsync(
        AppDbContext dbContext, string companyName, string eik, DateTime dateCreated)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Company,
            DateCreated = dateCreated,
            Company = new Company
            {
                Name = companyName,
                Eik = eik,
            },
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }
}
