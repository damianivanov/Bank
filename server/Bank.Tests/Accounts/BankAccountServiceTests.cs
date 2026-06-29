using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Accounts;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Accounts.BankAccounts;
using Bank.Services.Accounts.Iban;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Bank.Tests.Accounts;

public class BankAccountServiceTests
{
    private static BankAccountService BuildService(AppDbContext dbContext) =>
        new(dbContext, new IbanGenerator(), new FakeUserService());

    [Fact]
    public async Task CreateAccountAsync_WithValidRequest_OpensActiveAccountWithValidIban()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await SeedCustomerAsync(dbContext);
        var service = BuildService(dbContext);

        var result = await service.CreateAccountAsync(new CreateBankAccountRequest
        {
            CustomerId = customer.Id,
            OpeningBalance = 250m,
        });

        using var _ = new AssertionScope();
        result.Status.Should().Be(BankAccountStatus.Active);
        result.Balance.Should().Be(250m);
        result.CustomerId.Should().Be(customer.Id);
        result.ClosedAtUtc.Should().BeNull();
        result.Iban.Should().NotBeNullOrWhiteSpace();
        IbanValidator.IsValid(result.Iban).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAccountAsync_RoundsOpeningBalanceToTwoDecimals()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await SeedCustomerAsync(dbContext);
        var service = BuildService(dbContext);

        var result = await service.CreateAccountAsync(new CreateBankAccountRequest
        {
            CustomerId = customer.Id,
            OpeningBalance = 100.555m,
        });

        result.Balance.Should().Be(100.56m);
    }

    [Fact]
    public async Task CreateAccountAsync_WithNegativeOpeningBalance_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await SeedCustomerAsync(dbContext);
        var service = BuildService(dbContext);

        var act = () => service.CreateAccountAsync(new CreateBankAccountRequest
        {
            CustomerId = customer.Id,
            OpeningBalance = -1m,
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*не може да е отрицателно*");
    }

    [Fact]
    public async Task CreateAccountAsync_ForUnknownCustomer_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.CreateAccountAsync(new CreateBankAccountRequest
        {
            CustomerId = 999,
            OpeningBalance = 0m,
        });

        (await act.Should().ThrowAsync<BankException>())
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CloseAccountAsync_WithZeroBalance_ClosesAccount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await SeedCustomerAsync(dbContext);
        var service = BuildService(dbContext);
        var account = await service.CreateAccountAsync(new CreateBankAccountRequest { CustomerId = customer.Id, OpeningBalance = 0m });

        var result = await service.CloseAccountAsync(account.Id);

        using var _ = new AssertionScope();
        result.Status.Should().Be(BankAccountStatus.Closed);
        result.ClosedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task CloseAccountAsync_WithNonZeroBalance_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await SeedCustomerAsync(dbContext);
        var service = BuildService(dbContext);
        var account = await service.CreateAccountAsync(new CreateBankAccountRequest { CustomerId = customer.Id, OpeningBalance = 100m });

        var act = () => service.CloseAccountAsync(account.Id);

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*ненулево салдо*");
    }

    [Fact]
    public async Task CloseAccountAsync_WhenAlreadyClosed_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await SeedCustomerAsync(dbContext);
        var service = BuildService(dbContext);
        var account = await service.CreateAccountAsync(new CreateBankAccountRequest { CustomerId = customer.Id, OpeningBalance = 0m });
        await service.CloseAccountAsync(account.Id);

        var act = () => service.CloseAccountAsync(account.Id);

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*вече е закрита*");
    }

    [Fact]
    public async Task CloseAccountAsync_ForUnknownAccount_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.CloseAccountAsync(999);

        (await act.Should().ThrowAsync<BankException>())
            .Which.StatusCode.Should().Be(404);
    }

    private static readonly DateTime BaseOpenedAt = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAccountsAsync_ReturnsRequestedPageWithTotalCount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedAccountsAsync(dbContext, 25);
        var service = BuildService(dbContext);

        var firstPage = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20 });
        var secondPage = await service.GetAccountsAsync(new PagedRequest { Page = 2, PageSize = 20 });

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(25);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(20);
        firstPage.Items.Should().HaveCount(20);
        secondPage.Items.Should().HaveCount(5);
        secondPage.Page.Should().Be(2);
        // Страниците не трябва да се припокриват.
        firstPage.Items.Select(account => account.Id)
            .Should().NotIntersectWith(secondPage.Items.Select(account => account.Id));
    }

    [Fact]
    public async Task GetAccountsAsync_OrdersByOpenedAtDescending()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00OLD00000000000001", BaseOpenedAt);
        await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00NEW00000000000002", BaseOpenedAt.AddDays(5));
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20 });

        result.Items.First().Iban.Should().Be("BG00NEW00000000000002");
    }

    [Fact]
    public async Task GetAccountsAsync_ClampsExcessivePageSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedAccountsAsync(dbContext, 3);
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 500 });

        using var _ = new AssertionScope();
        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAccountsAsync_NormalizesNonPositivePageAndSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedAccountsAsync(dbContext, 3);
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 0, PageSize = 0 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAccountsAsync_SearchByIban_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00MATCH0000000001", BaseOpenedAt);
        await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00OTHER0000000002", BaseOpenedAt);
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "MATCH" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Iban.Should().Be("BG00MATCH0000000001");
    }

    [Fact]
    public async Task GetAccountsAsync_SearchByPersonName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00AAA00000000000001", BaseOpenedAt);
        await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00BBB00000000000002", BaseOpenedAt);
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Георгиева" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetAccountsAsync_SearchByCompanyName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedCompanyAccountAsync(dbContext, "Алфа Трейд ООД", "BG00CCC00000000000001", BaseOpenedAt);
        await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00DDD00000000000002", BaseOpenedAt);
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Алфа" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Алфа Трейд ООД");
    }

    [Fact]
    public async Task GetAccountsAsync_PopulatesCustomerDisplayName()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00EEE00000000000001", BaseOpenedAt);
        await SeedCompanyAccountAsync(dbContext, "Бета ЕООД", "BG00FFF00000000000002", BaseOpenedAt.AddMinutes(1));
        var service = BuildService(dbContext);

        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20 });

        // Имената на клиентите изискват Include на Person/Company; иначе колоната би била празна.
        result.Items.Select(account => account.CustomerDisplayName)
            .Should().Contain(["Иван Петров", "Бета ЕООД"]);
    }

    [Fact]
    public async Task GetAccountsAsync_SearchByFullName_MatchesAcrossNameBoundary()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00FULL00000000001", BaseOpenedAt);
        await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00FULL00000000002", BaseOpenedAt);
        var service = BuildService(dbContext);

        // Търсенето пресича границата между собствено и фамилно име благодарение на конкатенацията.
        var result = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "Иван Петров" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Иван Петров");
    }

    [Fact]
    public async Task GetAccountsAsync_SearchIsCaseInsensitive()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedIndividualAccountAsync(dbContext, "Иван", "Георгиев", "BG00CASE00000000001", BaseOpenedAt);
        var service = BuildService(dbContext);

        // Различен регистър от записаните данни — пинва нечувствителното към регистъра търсене.
        var byLowerName = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "георгиев" });
        var byLowerIban = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 20, Search = "bg00case" });

        using var _ = new AssertionScope();
        byLowerName.TotalCount.Should().Be(1);
        byLowerIban.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAccountsAsync_WithEqualOpenedAt_PagesDeterministicallyById()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        for (var index = 1; index <= 4; index++)
        {
            // Еднакво OpenedAtUtc за всички — страницирането трябва да разчита на вторичния ключ по Id.
            await SeedIndividualAccountAsync(dbContext, "Клиент", index.ToString(), $"BG00TIE{index:D14}", BaseOpenedAt);
        }
        var service = BuildService(dbContext);

        var firstPage = await service.GetAccountsAsync(new PagedRequest { Page = 1, PageSize = 2 });
        var secondPage = await service.GetAccountsAsync(new PagedRequest { Page = 2, PageSize = 2 });

        using var _ = new AssertionScope();
        var ids = firstPage.Items.Concat(secondPage.Items).Select(account => account.Id).ToArray();
        ids.Should().HaveCount(4);
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAccountsAsync_WithPageBeyondRange_ReturnsLastAvailablePage()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedAccountsAsync(dbContext, 3);
        var service = BuildService(dbContext);

        // Огромна стойност за Page не трябва да препълва int32 при изчисляване на отместването.
        var result = await service.GetAccountsAsync(new PagedRequest { Page = int.MaxValue, PageSize = 20 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    private static async Task SeedAccountsAsync(AppDbContext dbContext, int count)
    {
        for (var index = 0; index < count; index++)
        {
            dbContext.BankAccounts.Add(new BankAccount
            {
                IBAN = $"BG00SEED{index:D14}",
                Status = BankAccountStatus.Active,
                OpenedAtUtc = BaseOpenedAt.AddMinutes(index),
                Customer = new Customer
                {
                    CustomerType = CustomerType.Individual,
                    Person = new Person
                    {
                        FirstName = "Клиент",
                        LastName = index.ToString(),
                        Egn = Guid.NewGuid().ToString("N")[..10],
                    },
                },
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<BankAccount> SeedIndividualAccountAsync(
        AppDbContext dbContext, string firstName, string lastName, string iban, DateTime openedAtUtc)
    {
        var account = new BankAccount
        {
            IBAN = iban,
            Status = BankAccountStatus.Active,
            OpenedAtUtc = openedAtUtc,
            Customer = new Customer
            {
                CustomerType = CustomerType.Individual,
                Person = new Person
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Egn = Guid.NewGuid().ToString("N")[..10],
                },
            },
        };

        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    private static async Task<BankAccount> SeedCompanyAccountAsync(
        AppDbContext dbContext, string companyName, string iban, DateTime openedAtUtc)
    {
        var account = new BankAccount
        {
            IBAN = iban,
            Status = BankAccountStatus.Active,
            OpenedAtUtc = openedAtUtc,
            Customer = new Customer
            {
                CustomerType = CustomerType.Company,
                Company = new Company
                {
                    Name = companyName,
                    Eik = Guid.NewGuid().ToString("N")[..9],
                },
            },
        };

        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            Person = new Person { FirstName = "Elena", LastName = "Ivanova", Egn = Guid.NewGuid().ToString("N")[..10] },
            IsVip = false,
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }
}
