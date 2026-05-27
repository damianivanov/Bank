using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Accounts;

public class BankAccountModel
{
    public long Id { get; set; }
    public string Iban { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public BankAccountStatus Status { get; set; }
    public long CustomerId { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}
