using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

public class MoneyTransactionModel
{
    public long Id { get; set; }
    public long BankAccountId { get; set; }
    public MoneyTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public long? CreditId { get; set; }
    public long? CreditPaymentId { get; set; }
    public long? DepositRequestId { get; set; }
    public DateTime DateCreated { get; set; }
}
