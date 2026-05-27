using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreditModel
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public CreditType CreditType { get; set; }
    public decimal GrantedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal AppliedAnnualInterestRate { get; set; }
    public decimal AppliedGrantingFee { get; set; }
    public bool CustomerWasVipAtCreation { get; set; }
    public decimal PlannedMonthlyPaymentAmount { get; set; }
    public CreditStatus Status { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public DateTime? RepaidAtUtc { get; set; }
}
