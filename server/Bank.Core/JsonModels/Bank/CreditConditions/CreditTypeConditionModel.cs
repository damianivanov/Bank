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
    public PaymentType DefaultPaymentType { get; set; }
    public int PromoPeriodMonths { get; set; }
    public decimal? StandardPromoRate { get; set; }
    public decimal? VipPromoRate { get; set; }
    public int GracePeriodMonths { get; set; }
    public decimal StandardMonthlyManagementFee { get; set; }
    public decimal VipMonthlyManagementFee { get; set; }
    public decimal StandardAnnualManagementFee { get; set; }
    public decimal VipAnnualManagementFee { get; set; }
    public bool IsActive { get; set; }
}
