using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CustomerAccountSummaryModel
{
    public long Id { get; set; }
    public string Iban { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public BankAccountStatus Status { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}
