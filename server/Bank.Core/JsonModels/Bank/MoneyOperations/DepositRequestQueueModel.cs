using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

/// <summary>
/// Заявка за депозит, както я вижда служителят в опашката за одобрение — с данни за сметката и клиента.
/// </summary>
public class DepositRequestQueueModel
{
    public long Id { get; set; }
    public long BankAccountId { get; set; }
    public string AccountIban { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DepositRequestStatus Status { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime DateCreated { get; set; }
}
