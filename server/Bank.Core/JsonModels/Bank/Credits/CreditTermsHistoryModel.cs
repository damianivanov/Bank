using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

// Един запис от хронологията на условията по кредита — пряко отражение на ред в CreditTerms
// заедно с таксите му (CreditTermsFee). Не съдържа изчислени стойности; всичко тук е записано в базата.
public class CreditTermsHistoryModel
{
    public CreditTermsOrigin Origin { get; set; }
    public bool IsCurrent { get; set; }
    public int EffectiveFromPaymentNumber { get; set; }
    public DateTime DateCreated { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal BaseAnnualInterestRate { get; set; }
    public int PromoPeriodMonths { get; set; }
    public decimal? PromoAnnualInterestRate { get; set; }
    public int GracePeriodMonths { get; set; }
    public decimal Apr { get; set; }
    public bool WasVipApplied { get; set; }
    public decimal PlannedMonthlyPaymentAmount { get; set; }
    public IReadOnlyCollection<CreditTermsFeeModel> Fees { get; set; } = [];
}
