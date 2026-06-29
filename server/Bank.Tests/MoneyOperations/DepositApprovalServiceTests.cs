using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.MoneyOperations;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.MoneyOperations;

public class DepositApprovalServiceTests
{
    private static readonly DateTime BaseCreatedAt = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private static DepositApprovalService BuildService(AppDbContext dbContext, long staffUserId = 7) =>
        new(dbContext, new FakeUserService(staffUserId), new AccountLedger(dbContext));

    private static async Task<DepositRequest> SeedDepositRequestAsync(
        AppDbContext dbContext,
        long accountId,
        decimal amount,
        DepositRequestStatus status = DepositRequestStatus.Pending,
        DateTime? dateCreated = null)
    {
        var request = new DepositRequest
        {
            BankAccountId = accountId,
            Amount = amount,
            Status = status,
            IdempotencyKey = $"deposit-req:{Guid.NewGuid():N}",
            // Изричен DateCreated се запазва (auto-stamp-ва се само ако е default) — нужно е за детерминирано странициране.
            DateCreated = dateCreated ?? default,
        };

        dbContext.DepositRequests.Add(request);
        await dbContext.SaveChangesAsync();
        return request;
    }

    private static async Task<BankAccount> SeedIndividualAccountAsync(
        AppDbContext dbContext, string firstName, string lastName, string iban)
    {
        var account = new BankAccount
        {
            IBAN = iban,
            Status = BankAccountStatus.Active,
            OpenedAtUtc = BaseCreatedAt,
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
        AppDbContext dbContext, string companyName, string iban)
    {
        var account = new BankAccount
        {
            IBAN = iban,
            Status = BankAccountStatus.Active,
            OpenedAtUtc = BaseCreatedAt,
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

    [Fact]
    public async Task ApproveAsync_CreditsBalance_MarksApproved_AndRecordsDepositTransaction()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        var request = await SeedDepositRequestAsync(dbContext, account.Id, 1000m);
        var service = BuildService(dbContext, staffUserId: 7);

        var result = await service.ApproveAsync(request.Id);

        using var _ = new AssertionScope();
        result.NewBalance.Should().Be(1000m);
        result.Transaction.Type.Should().Be(MoneyTransactionType.Deposit);
        result.Transaction.DepositRequestId.Should().Be(request.Id);
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(1000m);

        var stored = await dbContext.DepositRequests.AsNoTracking().FirstAsync(d => d.Id == request.Id);
        stored.Status.Should().Be(DepositRequestStatus.Approved);
        stored.ReviewedById.Should().Be(7);
        stored.ReviewedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveAsync_CalledTwice_CreditsBalanceOnce()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        var request = await SeedDepositRequestAsync(dbContext, account.Id, 1000m);
        var service = BuildService(dbContext);

        var first = await service.ApproveAsync(request.Id);
        var second = await service.ApproveAsync(request.Id);

        using var _ = new AssertionScope();
        first.NewBalance.Should().Be(1000m);
        second.NewBalance.Should().Be(1000m);
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(1000m);
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ApproveAsync_RejectedRequest_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        var request = await SeedDepositRequestAsync(dbContext, account.Id, 1000m, DepositRequestStatus.Rejected);
        var service = BuildService(dbContext);

        var act = () => service.ApproveAsync(request.Id);

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*чакащи*");
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(0m);
    }

    [Fact]
    public async Task ApproveAsync_UnknownRequest_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.ApproveAsync(999);

        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RejectAsync_MarksRejected_WithoutChangingBalance()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        var request = await SeedDepositRequestAsync(dbContext, account.Id, 1000m);
        var service = BuildService(dbContext);

        var result = await service.RejectAsync(request.Id, new DepositRejectRequest { Note = "Подозрителна сума" });

        using var _ = new AssertionScope();
        result.Status.Should().Be(DepositRequestStatus.Rejected);
        result.ReviewNote.Should().Be("Подозрителна сума");
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(0m);
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RejectAsync_AlreadyApprovedRequest_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        var request = await SeedDepositRequestAsync(dbContext, account.Id, 1000m, DepositRequestStatus.Approved);
        var service = BuildService(dbContext);

        var act = () => service.RejectAsync(request.Id, new DepositRejectRequest());

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*чакащи*");
    }

    [Fact]
    public async Task GetDepositRequestsAsync_FiltersByStatus()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        await SeedDepositRequestAsync(dbContext, account.Id, 100m, DepositRequestStatus.Pending);
        await SeedDepositRequestAsync(dbContext, account.Id, 200m, DepositRequestStatus.Rejected);
        var service = BuildService(dbContext);

        var pending = await service.GetDepositRequestsAsync(DepositRequestStatus.Pending, new PagedRequest { Page = 1, PageSize = 20 });
        var all = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20 });

        using var _ = new AssertionScope();
        pending.TotalCount.Should().Be(1);
        pending.Items.Should().ContainSingle().Which.Status.Should().Be(DepositRequestStatus.Pending);
        pending.Items.Single().CustomerDisplayName.Should().Be("Ivan Petrov");
        all.TotalCount.Should().Be(2);
        all.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_ReturnsRequestedPageWithTotalCount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        for (var index = 0; index < 25; index++)
        {
            await SeedDepositRequestAsync(dbContext, account.Id, 100m + index, dateCreated: BaseCreatedAt.AddMinutes(index));
        }
        var service = BuildService(dbContext);

        var firstPage = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20 });
        var secondPage = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 2, PageSize = 20 });

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(25);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(20);
        firstPage.Items.Should().HaveCount(20);
        secondPage.Items.Should().HaveCount(5);
        secondPage.Page.Should().Be(2);
        // Страниците не трябва да се припокриват.
        firstPage.Items.Select(d => d.Id)
            .Should().NotIntersectWith(secondPage.Items.Select(d => d.Id));
    }

    [Fact]
    public async Task GetDepositRequestsAsync_OrdersByDateCreatedDescending()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        await SeedDepositRequestAsync(dbContext, account.Id, 100m, dateCreated: BaseCreatedAt);
        var newest = await SeedDepositRequestAsync(dbContext, account.Id, 200m, dateCreated: BaseCreatedAt.AddDays(5));
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20 });

        result.Items.First().Id.Should().Be(newest.Id);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_ClampsExcessivePageSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        for (var index = 0; index < 3; index++)
        {
            await SeedDepositRequestAsync(dbContext, account.Id, 100m + index, dateCreated: BaseCreatedAt.AddMinutes(index));
        }
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 500 });

        using var _ = new AssertionScope();
        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_NormalizesNonPositivePageAndSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        for (var index = 0; index < 3; index++)
        {
            await SeedDepositRequestAsync(dbContext, account.Id, 100m + index, dateCreated: BaseCreatedAt.AddMinutes(index));
        }
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 0, PageSize = 0 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_WithPageBeyondRange_ReturnsLastAvailablePage()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        for (var index = 0; index < 3; index++)
        {
            await SeedDepositRequestAsync(dbContext, account.Id, 100m + index, dateCreated: BaseCreatedAt.AddMinutes(index));
        }
        var service = BuildService(dbContext);

        // Огромна стойност за Page не трябва да препълва int32 при изчисляване на отместването.
        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = int.MaxValue, PageSize = 20 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_SearchByIban_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var matchAccount = await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00MATCH0000000001");
        var otherAccount = await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00OTHER0000000002");
        await SeedDepositRequestAsync(dbContext, matchAccount.Id, 100m, dateCreated: BaseCreatedAt);
        await SeedDepositRequestAsync(dbContext, otherAccount.Id, 200m, dateCreated: BaseCreatedAt);
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20, Search = "MATCH" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.AccountIban.Should().Be("BG00MATCH0000000001");
    }

    [Fact]
    public async Task GetDepositRequestsAsync_SearchByPersonName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var ivanAccount = await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00AAA00000000000001");
        var mariaAccount = await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00BBB00000000000002");
        await SeedDepositRequestAsync(dbContext, ivanAccount.Id, 100m, dateCreated: BaseCreatedAt);
        await SeedDepositRequestAsync(dbContext, mariaAccount.Id, 200m, dateCreated: BaseCreatedAt);
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20, Search = "Георгиева" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Мария Георгиева");
    }

    [Fact]
    public async Task GetDepositRequestsAsync_SearchByCompanyName_ReturnsOnlyMatches()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var companyAccount = await SeedCompanyAccountAsync(dbContext, "Алфа Трейд ООД", "BG00CCC00000000000001");
        var individualAccount = await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00DDD00000000000002");
        await SeedDepositRequestAsync(dbContext, companyAccount.Id, 100m, dateCreated: BaseCreatedAt);
        await SeedDepositRequestAsync(dbContext, individualAccount.Id, 200m, dateCreated: BaseCreatedAt);
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20, Search = "Алфа" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.CustomerDisplayName.Should().Be("Алфа Трейд ООД");
    }

    [Fact]
    public async Task GetDepositRequestsAsync_SearchIsCaseInsensitive()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var account = await SeedIndividualAccountAsync(dbContext, "Иван", "Георгиев", "BG00CASE00000000001");
        await SeedDepositRequestAsync(dbContext, account.Id, 100m, dateCreated: BaseCreatedAt);
        var service = BuildService(dbContext);

        // Различен регистър от записаните данни — пинва нечувствителното към регистъра търсене.
        var byLowerName = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20, Search = "георгиев" });
        var byLowerIban = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 20, Search = "bg00case" });

        using var _ = new AssertionScope();
        byLowerName.TotalCount.Should().Be(1);
        byLowerIban.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_AppliesStatusFilterAndSearchTogether()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var ivanAccount = await SeedIndividualAccountAsync(dbContext, "Иван", "Петров", "BG00AAA00000000000001");
        var mariaAccount = await SeedIndividualAccountAsync(dbContext, "Мария", "Георгиева", "BG00BBB00000000000002");
        // Чакаща + отхвърлена за Иван; чакаща за Мария. Само статус Pending И търсене "Петров" трябва да съвпадне с първата.
        await SeedDepositRequestAsync(dbContext, ivanAccount.Id, 100m, DepositRequestStatus.Pending, BaseCreatedAt);
        await SeedDepositRequestAsync(dbContext, ivanAccount.Id, 150m, DepositRequestStatus.Rejected, BaseCreatedAt.AddMinutes(1));
        await SeedDepositRequestAsync(dbContext, mariaAccount.Id, 200m, DepositRequestStatus.Pending, BaseCreatedAt.AddMinutes(2));
        var service = BuildService(dbContext);

        var result = await service.GetDepositRequestsAsync(
            DepositRequestStatus.Pending,
            new PagedRequest { Page = 1, PageSize = 20, Search = "Петров" });

        using var _ = new AssertionScope();
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Status.Should().Be(DepositRequestStatus.Pending);
        result.Items.Single().CustomerDisplayName.Should().Be("Иван Петров");
        result.Items.Single().Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetDepositRequestsAsync_WithEqualDateCreated_PagesDeterministicallyById()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 0m);
        // Еднакво DateCreated за всички — страницирането трябва да разчита на вторичния ключ по Id.
        for (var index = 0; index < 4; index++)
        {
            await SeedDepositRequestAsync(dbContext, account.Id, 100m + index, dateCreated: BaseCreatedAt);
        }
        var service = BuildService(dbContext);

        var firstPage = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 1, PageSize = 2 });
        var secondPage = await service.GetDepositRequestsAsync(null, new PagedRequest { Page = 2, PageSize = 2 });

        using var _ = new AssertionScope();
        var ids = firstPage.Items.Concat(secondPage.Items).Select(d => d.Id).ToArray();
        ids.Should().HaveCount(4);
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().BeInDescendingOrder();
    }
}
