using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.DB;
using Bank.DB.Entities;

namespace Bank.Services.MoneyOperations;

public class AccountLedger : IAccountLedger
{
    private readonly AppDbContext dbContext;

    public AccountLedger(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public MoneyTransaction Record(LedgerEntry entry)
    {
        var account = entry.Account;
        var roundedAmount = decimal.Round(entry.Amount, 2, MidpointRounding.AwayFromZero);
        if (roundedAmount <= 0m)
        {
            throw new BankException("Сумата на транзакцията трябва да е по-голяма от нула.");
        }

        var delta = entry.Type == MoneyTransactionType.Deposit ? roundedAmount : -roundedAmount;
        var newBalance = decimal.Round(account.Balance + delta, 2, MidpointRounding.AwayFromZero);

        // Защитна мрежа — извикващият вече е проверил достатъчност, но регистърът никога не оставя сметка на минус.
        if (newBalance < 0m)
        {
            throw new BankException("Недостатъчна наличност за тази операция.");
        }

        account.Balance = newBalance;

        var transaction = new MoneyTransaction
        {
            BankAccountId = account.Id,
            BankAccount = account,
            Type = entry.Type,
            Amount = roundedAmount,
            BalanceAfter = newBalance,
            CreditId = entry.CreditId,
            CreditPaymentId = entry.CreditPaymentId,
            DepositRequestId = entry.DepositRequestId,
            IdempotencyKey = entry.IdempotencyKey,
            RequestHash = entry.RequestHash,
        };

        dbContext.MoneyTransactions.Add(transaction);
        return transaction;
    }
}
