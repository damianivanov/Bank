using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Customers;

public class CustomerCreditSummaryModel
{
    public long Id { get; set; }
    public CreditType CreditType { get; set; }
    public decimal GrantedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal AppliedAnnualInterestRate { get; set; }
    public decimal PlannedMonthlyPaymentAmount { get; set; }
    public CreditStatus Status { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public DateTime? RepaidAtUtc { get; set; }
}
