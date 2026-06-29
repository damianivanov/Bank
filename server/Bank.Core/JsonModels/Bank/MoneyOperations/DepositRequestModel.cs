using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

/// <summary>
/// Заявка за депозит, както я вижда клиентът-заявител.
/// </summary>
public class DepositRequestModel
{
    public long Id { get; set; }
    public long BankAccountId { get; set; }
    public string AccountIban { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DepositRequestStatus Status { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime DateCreated { get; set; }
}
