using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.CreditConditions;

public class CreditTypeConditionModel
{
    public long Id { get; set; }
    public CreditType CreditType { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal StandardAnnualInterestRate { get; set; }
    public decimal VipAnnualInterestRate { get; set; }
    public decimal MaximumAmount { get; set; }
    public int MaximumTermMonths { get; set; }
    public decimal StandardGrantingFee { get; set; }
    public decimal VipGrantingFee { get; set; }
    public bool IsActive { get; set; }
}
