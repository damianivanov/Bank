using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreditPricingChangeModel
{
    public long Id { get; set; }
    public decimal PreviousAnnualInterestRate { get; set; }
    public decimal NewAnnualInterestRate { get; set; }
    public int EffectiveFromPaymentNumber { get; set; }
    public PricingChangeReason Reason { get; set; }
    public DateTime DateCreated { get; set; }
}
