using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.Services.MoneyOperations;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.MoneyOperations;

public class MoneyOperationServiceTests
{
    private static MoneyOperationService BuildService(AppDbContext dbContext, long userId = 1, bool allowFutureInstallments = false) =>
        new(dbContext, new FakeUserService(userId), new AccountLedger(dbContext),
            new Bank.Core.Settings.DemoOptions { AllowPayingFutureInstallments = allowFutureInstallments });

    private static IReadOnlyCollection<long> Owns(params long[] customerIds) => customerIds;

    [Fact]
    public async Task RequestDepositAsync_CreatesPendingRequest_WithoutChangingBalance()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        var service = BuildService(dbContext);

        var result = await service.RequestDepositAsync(Owns(customer.Id), account.Id, new DepositRequestCreateRequest
        {
            Amount = 5000m,
            IdempotencyKey = "deposit-key-0001",
        });

        using var _ = new AssertionScope();
        result.Status.Should().Be(DepositRequestStatus.Pending);
        result.Amount.Should().Be(5000m);
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(100m);
        (await dbContext.DepositRequests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RequestDepositAsync_ForForeignAccount_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        var service = BuildService(dbContext);

        // Влезлият клиент притежава customer id 999, а не клиента на сметката.
        var act = () => service.RequestDepositAsync(Owns(999), account.Id, new DepositRequestCreateRequest
        {
            Amount = 100m,
            IdempotencyKey = "deposit-key-0002",
        });

        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RequestDepositAsync_SameIdempotencyKeyTwice_CreatesSingleRequest()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id);
        var service = BuildService(dbContext);

        var request = new DepositRequestCreateRequest { Amount = 250m, IdempotencyKey = "same-deposit-key" };
        var first = await service.RequestDepositAsync(Owns(customer.Id), account.Id, request);
        var second = await service.RequestDepositAsync(Owns(customer.Id), account.Id, request);

        using var _ = new AssertionScope();
        second.Id.Should().Be(first.Id);
        (await dbContext.DepositRequests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RequestDepositAsync_SameKeyDifferentAmount_ThrowsConflict()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id);
        var service = BuildService(dbContext);

        // Същият ключ, но различна сума -> опит за подмяна на тялото на заявката, а не честен retry.
        await service.RequestDepositAsync(Owns(customer.Id), account.Id,
            new DepositRequestCreateRequest { Amount = 250m, IdempotencyKey = "reuse-deposit-key" });

        var act = () => service.RequestDepositAsync(Owns(customer.Id), account.Id,
            new DepositRequestCreateRequest { Amount = 5000m, IdempotencyKey = "reuse-deposit-key" });

        using var _ = new AssertionScope();
        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(409);
        (await dbContext.DepositRequests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WithdrawAsync_WithSufficientBalance_DecrementsBalanceAndRecordsTransaction()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 500m);
        var service = BuildService(dbContext);

        var result = await service.WithdrawAsync(Owns(customer.Id), account.Id, new WithdrawalCreateRequest
        {
            Amount = 200m,
            IdempotencyKey = "withdraw-key-0001",
        });

        using var _ = new AssertionScope();
        result.NewBalance.Should().Be(300m);
        result.Transaction.Type.Should().Be(MoneyTransactionType.Withdrawal);
        result.Transaction.Amount.Should().Be(200m);
        result.Transaction.BalanceAfter.Should().Be(300m);
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(300m);
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WithdrawAsync_WithInsufficientBalance_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 50m);
        var service = BuildService(dbContext);

        var act = () => service.WithdrawAsync(Owns(customer.Id), account.Id, new WithdrawalCreateRequest
        {
            Amount = 200m,
            IdempotencyKey = "withdraw-key-0002",
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*Недостатъчна наличност*");
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task WithdrawAsync_OnClosedAccount_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 500m, status: BankAccountStatus.Closed);
        var service = BuildService(dbContext);

        var act = () => service.WithdrawAsync(Owns(customer.Id), account.Id, new WithdrawalCreateRequest
        {
            Amount = 100m,
            IdempotencyKey = "withdraw-key-0003",
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*не е активна*");
    }

    [Fact]
    public async Task WithdrawAsync_ForForeignAccount_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 500m);
        var service = BuildService(dbContext);

        var act = () => service.WithdrawAsync(Owns(12345), account.Id, new WithdrawalCreateRequest
        {
            Amount = 100m,
            IdempotencyKey = "withdraw-key-0004",
        });

        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task WithdrawAsync_SameIdempotencyKeyTwice_WithdrawsOnce()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 500m);
        var service = BuildService(dbContext);

        var request = new WithdrawalCreateRequest { Amount = 200m, IdempotencyKey = "same-withdraw-key" };
        var first = await service.WithdrawAsync(Owns(customer.Id), account.Id, request);
        var second = await service.WithdrawAsync(Owns(customer.Id), account.Id, request);

        using var _ = new AssertionScope();
        first.NewBalance.Should().Be(300m);
        second.NewBalance.Should().Be(300m);
        second.Transaction.Id.Should().Be(first.Transaction.Id);
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(300m);
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WithdrawAsync_SameKeyDifferentAmount_ThrowsConflict()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 500m);
        var service = BuildService(dbContext);

        // Първото теглене минава; повторното със същия ключ, но друга сума, е конфликт, не тих replay.
        await service.WithdrawAsync(Owns(customer.Id), account.Id,
            new WithdrawalCreateRequest { Amount = 200m, IdempotencyKey = "reuse-withdraw-key" });

        var act = () => service.WithdrawAsync(Owns(customer.Id), account.Id,
            new WithdrawalCreateRequest { Amount = 50m, IdempotencyKey = "reuse-withdraw-key" });

        using var _ = new AssertionScope();
        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(409);
        // Салдото е намаляло само с първото теглене — конфликтът не извършва ново движение.
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(300m);
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_PaysNextInstallment_DecrementsBalanceAndMarksPaid()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 300m, 300m);
        var service = BuildService(dbContext);

        var result = await service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = account.Id,
            IdempotencyKey = "pay-key-0001",
        });

        using var _ = new AssertionScope();
        result.NewBalance.Should().Be(700m);
        result.Payment.Status.Should().Be(CreditPaymentStatus.Paid);
        result.Payment.PaymentNumber.Should().Be(1);
        result.CreditStatus.Should().Be(CreditStatus.Active);
        result.Transaction.Type.Should().Be(MoneyTransactionType.CreditPayment);
        result.Transaction.CreditId.Should().Be(credit.Id);
        (await dbContext.BankAccounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance.Should().Be(700m);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_LastInstallment_MarksCreditRepaid()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 300m);
        var service = BuildService(dbContext);

        var result = await service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = account.Id,
            IdempotencyKey = "pay-key-0002",
        });

        using var _ = new AssertionScope();
        result.CreditStatus.Should().Be(CreditStatus.Repaid);
        result.CreditRepaidAtUtc.Should().NotBeNull();
        (await dbContext.Credits.AsNoTracking().FirstAsync(c => c.Id == credit.Id)).Status.Should().Be(CreditStatus.Repaid);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_AutoSelectsSingleActiveAccount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 250m, 250m);
        var service = BuildService(dbContext);

        // Без FundingAccountId — единствената активна сметка трябва да се избере автоматично.
        var result = await service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = null,
            IdempotencyKey = "pay-key-0003",
        });

        result.AccountId.Should().Be(account.Id);
        result.NewBalance.Should().Be(750m);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_MultipleActiveAccounts_WithoutSelection_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 250m);
        var service = BuildService(dbContext);

        var act = () => service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = null,
            IdempotencyKey = "pay-key-0004",
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*изберете от коя сметка*");
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_InsufficientBalance_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 300m);
        var service = BuildService(dbContext);

        var act = () => service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = account.Id,
            IdempotencyKey = "pay-key-0005",
        });

        (await act.Should().ThrowAsync<BankException>()).WithMessage("*Недостатъчна наличност*");
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_ForForeignCredit_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 250m);
        var service = BuildService(dbContext);

        var act = () => service.PayCreditInstallmentAsync(Owns(4242), credit.Id, new PayCreditInstallmentRequest
        {
            IdempotencyKey = "pay-key-0006",
        });

        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_SameKeyDifferentFundingAccount_ThrowsConflict()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var accountA = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var accountB = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(dbContext, customer.Id, 300m, 300m);
        var service = BuildService(dbContext);

        // Първата вноска се плаща от сметка A; повторното със същия ключ, но друга сметка, е различно намерение.
        await service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id,
            new PayCreditInstallmentRequest { FundingAccountId = accountA.Id, IdempotencyKey = "reuse-pay-key" });

        var act = () => service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id,
            new PayCreditInstallmentRequest { FundingAccountId = accountB.Id, IdempotencyKey = "reuse-pay-key" });

        using var _ = new AssertionScope();
        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(409);
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_WhenNextInstallmentNotYetDue_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        // Първата вноска е с падеж следващия месец -> още не е дължима за клиента.
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(
            dbContext, customer.Id, firstInstallmentMonthOffset: 1, installmentAmounts: new[] { 300m });
        var service = BuildService(dbContext);

        var act = () => service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = account.Id,
            IdempotencyKey = "pay-future-0001",
        });

        using var _ = new AssertionScope();
        (await act.Should().ThrowAsync<BankException>()).WithMessage("*падеж*");
        (await dbContext.MoneyTransactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PayCreditInstallmentAsync_WithDevBypass_AllowsPayingNotYetDueInstallment()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 1000m);
        var credit = await MoneyOperationsTestData.SeedActiveCreditAsync(
            dbContext, customer.Id, firstInstallmentMonthOffset: 1, installmentAmounts: new[] { 300m });
        // dev разрешителят позволява предсрочно плащане (за тест/демо на целия процес).
        var service = BuildService(dbContext, allowFutureInstallments: true);

        var result = await service.PayCreditInstallmentAsync(Owns(customer.Id), credit.Id, new PayCreditInstallmentRequest
        {
            FundingAccountId = account.Id,
            IdempotencyKey = "pay-future-bypass-0001",
        });

        using var _ = new AssertionScope();
        result.Payment.Status.Should().Be(CreditPaymentStatus.Paid);
        result.NewBalance.Should().Be(700m);
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_ForForeignAccount_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        var service = BuildService(dbContext);

        var act = () => service.GetAccountTransactionsAsync(Owns(7777), account.Id, new PagedRequest { Page = 1, PageSize = 20 });

        (await act.Should().ThrowAsync<BankException>()).Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_ReturnsRequestedPageWithTotalCount()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, account.Id, 25);
        var service = BuildService(dbContext);

        var firstPage = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = 1, PageSize = 20 });
        var secondPage = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = 2, PageSize = 20 });

        using var _ = new AssertionScope();
        firstPage.TotalCount.Should().Be(25);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(20);
        firstPage.Items.Should().HaveCount(20);
        secondPage.Items.Should().HaveCount(5);
        secondPage.Page.Should().Be(2);
        // Страниците не трябва да се припокриват.
        firstPage.Items.Select(transaction => transaction.Id)
            .Should().NotIntersectWith(secondPage.Items.Select(transaction => transaction.Id));
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_OrdersByDateCreatedDescending()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        // Анкерът+минути прави последното движение най-ново — то трябва да е първо в резултата.
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, account.Id, 5);
        var service = BuildService(dbContext);

        var result = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = 1, PageSize = 20 });

        result.Items.Select(transaction => transaction.DateCreated).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_ClampsExcessivePageSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, account.Id, 3);
        var service = BuildService(dbContext);

        var result = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = 1, PageSize = 500 });

        using var _ = new AssertionScope();
        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_NormalizesNonPositivePageAndSize()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, account.Id, 3);
        var service = BuildService(dbContext);

        var result = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = 0, PageSize = 0 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_WithPageBeyondRange_ReturnsLastAvailablePage()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, account.Id, 3);
        var service = BuildService(dbContext);

        // Огромна стойност за Page не трябва да препълва int32 при изчисляване на отместването.
        var result = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = int.MaxValue, PageSize = 20 });

        using var _ = new AssertionScope();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_ExcludesTransactionsFromOtherAccounts()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var customer = await MoneyOperationsTestData.SeedCustomerAsync(dbContext);
        var account = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        var otherAccount = await MoneyOperationsTestData.SeedAccountAsync(dbContext, customer.Id, balance: 100m);
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, account.Id, 2);
        await MoneyOperationsTestData.SeedTransactionsAsync(dbContext, otherAccount.Id, 5);
        var service = BuildService(dbContext);

        var result = await service.GetAccountTransactionsAsync(Owns(customer.Id), account.Id, new PagedRequest { Page = 1, PageSize = 20 });

        // Само движенията по заявената сметка влизат в общия брой и резултата.
        result.TotalCount.Should().Be(2);
    }
}
