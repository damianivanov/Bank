using Bank.Core.Enums;
using Bank.DB;
using Bank.DB.Entities;

namespace Bank.Tests.MoneyOperations;

internal static class MoneyOperationsTestData
{
    public static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            Person = new Person { FirstName = "Ivan", LastName = "Petrov", Egn = Guid.NewGuid().ToString("N")[..10] },
            IsVip = false,
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    public static async Task<BankAccount> SeedAccountAsync(
        AppDbContext dbContext,
        long customerId,
        decimal balance = 0m,
        BankAccountStatus status = BankAccountStatus.Active)
    {
        var account = new BankAccount
        {
            IBAN = $"BG{Guid.NewGuid().ToString("N")[..20].ToUpperInvariant()}",
            Balance = balance,
            Status = status,
            CustomerId = customerId,
            OpenedAtUtc = DateTime.UtcNow,
        };

        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    public static async Task SeedTransactionsAsync(
        AppDbContext dbContext,
        long accountId,
        int count,
        DateTime? baseDateCreated = null)
    {
        // Явно подаваме DateCreated, за да е страницирането по време детерминирано в тестовете
        // (AppDbContext го пази, щом не е default). По-новите движения са с по-голямо отместване в минути.
        var anchor = baseDateCreated ?? new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        for (var index = 0; index < count; index++)
        {
            dbContext.MoneyTransactions.Add(new MoneyTransaction
            {
                BankAccountId = accountId,
                Type = MoneyTransactionType.Withdrawal,
                Amount = 10m,
                BalanceAfter = 0m,
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                DateCreated = anchor.AddMinutes(index),
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public static Task<Credit> SeedActiveCreditAsync(
        AppDbContext dbContext,
        long customerId,
        params decimal[] installmentAmounts)
        => SeedActiveCreditAsync(dbContext, customerId, firstInstallmentMonthOffset: 0, installmentAmounts);

    public static async Task<Credit> SeedActiveCreditAsync(
        AppDbContext dbContext,
        long customerId,
        int firstInstallmentMonthOffset,
        decimal[] installmentAmounts)
    {
        var credit = new Credit
        {
            CustomerId = customerId,
            CreditTypeConditionId = 1,
            GrantedAmount = installmentAmounts.Sum(),
            TermMonths = installmentAmounts.Length,
            AppliedAnnualInterestRate = 10m,
            AppliedGrantingFee = 0m,
            PlannedMonthlyPaymentAmount = installmentAmounts.Length > 0 ? installmentAmounts[0] : 0m,
            Status = CreditStatus.Active,
            GrantedAtUtc = DateTime.UtcNow,
        };

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync();

        for (var index = 0; index < installmentAmounts.Length; index++)
        {
            dbContext.CreditInstallments.Add(new CreditInstallment
            {
                CreditId = credit.Id,
                InstallmentNumber = index + 1,
                DueDate = DateTime.UtcNow.Date.AddMonths(index + firstInstallmentMonthOffset),
                InstallmentAmount = installmentAmounts[index],
                PrincipalPart = installmentAmounts[index],
                InterestPart = 0m,
                RemainingPrincipalAfterPayment = installmentAmounts.Skip(index + 1).Sum(),
                Status = CreditPaymentStatus.Pending,
            });
        }

        await dbContext.SaveChangesAsync();
        return credit;
    }
}
