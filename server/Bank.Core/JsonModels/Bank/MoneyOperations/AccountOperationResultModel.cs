namespace Bank.Core.JsonModels.Bank.MoneyOperations;

/// <summary>
/// Резултат от движение по сметка (теглене или одобрен депозит): новото салдо и записаното движение.
/// </summary>
public class AccountOperationResultModel
{
    public long AccountId { get; set; }
    public string AccountIban { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public MoneyTransactionModel Transaction { get; set; } = new();
}
