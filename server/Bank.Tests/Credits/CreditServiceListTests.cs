using Bank.Core.Enums;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Credits;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Bank.Tests.Credits;

public class CreditServiceListTests
{
    private static readonly DateTime BaseGrantedAt = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private static CreditService BuildService(AppDbContext dbContext) =>
        new(dbContext, new FakeUserService(), new CreditCalculatorService(TimeProvider.System), new Bank.Core.Settings.DemoOptions());

    [Fact]
    public async Task GetCreditsAsync_ReturnsRequestedPageWithTotalCount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCreditsAsync(dbContext, 25);
        var service = BuildService(dbContext);

        var firstPage = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20 });
        var secondPage = await service.GetCreditsAsync(new PagedRequest { Page = 2, PageSize = 20 });

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(25);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(20);
        firstPage.Items.Should().HaveCount(20);
        secondPage.Items.Should().HaveCount(5);
        secondPage.Page.Should().Be(2);
        // Страниците не трябва да се припокриват.
        firstPage.Items.Select(credit => credit.Id)
            .Should().NotIntersectWith(secondPage.Items.Select(credit => credit.Id));
    }

    [Fact]
    public async Task GetCreditsAsync_OrdersByGrantedAtDescending()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        await SeedIndividualCreditAsync(dbContext, condition, "Иван", "Петров", BaseGrantedAt);
        await SeedIndividualCreditAsync(dbContext, condition, "Мария", "Георгиева", BaseGrantedAt.AddDays(5));
        var service = BuildService(dbContext);

        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20 });

        result.Items.First().CustomerDisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetCreditsAsync_ClampsExcessivePageSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCreditsAsync(dbContext, 3);
        var service = BuildService(dbContext);

        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 500 });

        using var _ = new AssertionScope();
        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCreditsAsync_NormalizesNonPositivePageAndSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCreditsAsync(dbContext, 3);
        var service = BuildService(dbContext);

        var result = await service.GetCreditsAsync(new PagedRequest { Page = 0, PageSize = 0 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetCreditsAsync_SearchByPersonName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        await SeedIndividualCreditAsync(dbContext, condition, "Иван", "Петров", BaseGrantedAt);
        await SeedIndividualCreditAsync(dbContext, condition, "Мария", "Георгиева", BaseGrantedAt);
        var service = BuildService(dbContext);

        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Георгиева" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetCreditsAsync_SearchByCompanyName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        await SeedCompanyCreditAsync(dbContext, condition, "Алфа Трейд ООД", BaseGrantedAt);
        await SeedIndividualCreditAsync(dbContext, condition, "Иван", "Петров", BaseGrantedAt);
        var service = BuildService(dbContext);

        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Алфа" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Алфа Трейд ООД");
    }

    [Fact]
    public async Task GetCreditsAsync_SearchByFullName_MatchesAcrossNameBoundary()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        await SeedIndividualCreditAsync(dbContext, condition, "Иван", "Петров", BaseGrantedAt);
        await SeedIndividualCreditAsync(dbContext, condition, "Мария", "Георгиева", BaseGrantedAt);
        var service = BuildService(dbContext);

        // Търсенето пресича границата между собствено и фамилно име благодарение на конкатенацията.
        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Иван Петров" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Иван Петров");
    }

    [Fact]
    public async Task GetCreditsAsync_SearchIsCaseInsensitive()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        await SeedIndividualCreditAsync(dbContext, condition, "Иван", "Георгиев", BaseGrantedAt);
        var service = BuildService(dbContext);

        // Различен регистър от записаните данни — пинва нечувствителното към регистъра търсене.
        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "георгиев" });

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCreditsAsync_PopulatesCustomerDisplayName()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        await SeedIndividualCreditAsync(dbContext, condition, "Иван", "Петров", BaseGrantedAt);
        await SeedCompanyCreditAsync(dbContext, condition, "Бета ЕООД", BaseGrantedAt.AddMinutes(1));
        var service = BuildService(dbContext);

        var result = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 20 });

        // Имената на клиентите изискват Include на Person/Company; иначе колоната би била празна.
        result.Items.Select(credit => credit.CustomerDisplayName)
            .Should().Contain(["Иван Петров", "Бета ЕООД"]);
    }

    [Fact]
    public async Task GetCreditsAsync_WithEqualGrantedAt_PagesDeterministicallyById()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        for (var index = 1; index <= 4; index++)
        {
            // Еднакво GrantedAtUtc за всички — страницирането трябва да разчита на вторичния ключ по Id.
            await SeedIndividualCreditAsync(dbContext, condition, "Клиент", index.ToString(), BaseGrantedAt);
        }
        var service = BuildService(dbContext);

        var firstPage = await service.GetCreditsAsync(new PagedRequest { Page = 1, PageSize = 2 });
        var secondPage = await service.GetCreditsAsync(new PagedRequest { Page = 2, PageSize = 2 });

        using var _ = new AssertionScope();
        var ids = firstPage.Items.Concat(secondPage.Items).Select(credit => credit.Id).ToArray();
        ids.Should().HaveCount(4);
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetCreditsAsync_WithPageBeyondRange_ReturnsLastAvailablePage()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCreditsAsync(dbContext, 3);
        var service = BuildService(dbContext);

        // Огромна стойност за Page не трябва да препълва int32 при изчисляване на отместването.
        var result = await service.GetCreditsAsync(new PagedRequest { Page = int.MaxValue, PageSize = 20 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    private static async Task SeedCreditsAsync(AppDbContext dbContext, int count)
    {
        var condition = await SeedConditionAsync(dbContext);
        for (var index = 0; index < count; index++)
        {
            dbContext.Credits.Add(BuildCredit(condition, BaseGrantedAt.AddMinutes(index), new Customer
            {
                CustomerType = CustomerType.Individual,
                Person = new Person
                {
                    FirstName = "Клиент",
                    LastName = index.ToString(),
                    Egn = Guid.NewGuid().ToString("N")[..10],
                },
            }));
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Credit> SeedIndividualCreditAsync(
        AppDbContext dbContext, CreditTypeCondition condition, string firstName, string lastName, DateTime grantedAtUtc)
    {
        var credit = BuildCredit(condition, grantedAtUtc, new Customer
        {
            CustomerType = CustomerType.Individual,
            Person = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                Egn = Guid.NewGuid().ToString("N")[..10],
            },
        });

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync();
        return credit;
    }

    private static async Task<Credit> SeedCompanyCreditAsync(
        AppDbContext dbContext, CreditTypeCondition condition, string companyName, DateTime grantedAtUtc)
    {
        var credit = BuildCredit(condition, grantedAtUtc, new Customer
        {
            CustomerType = CustomerType.Company,
            Company = new Company
            {
                Name = companyName,
                Eik = Guid.NewGuid().ToString("N")[..9],
            },
        });

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync();
        return credit;
    }

    private static Credit BuildCredit(CreditTypeCondition condition, DateTime grantedAtUtc, Customer customer) => new()
    {
        Customer = customer,
        CreditTypeCondition = condition,
        GrantedAmount = 10000m,
        TermMonths = 12,
        AppliedAnnualInterestRate = 8.5m,
        AppliedGrantingFee = 120m,
        CustomerWasVipAtCreation = false,
        PlannedMonthlyPaymentAmount = 870m,
        Status = CreditStatus.Active,
        GrantedAtUtc = grantedAtUtc,
    };

    private static async Task<CreditTypeCondition> SeedConditionAsync(AppDbContext dbContext)
    {
        var condition = new CreditTypeCondition
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
        };

        dbContext.CreditTypeConditions.Add(condition);
        await dbContext.SaveChangesAsync();
        return condition;
    }
}
