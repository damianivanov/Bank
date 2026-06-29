using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreditTermsModel
{
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

public class CreditTermsFeeModel
{
    public CreditFeeKind Kind { get; set; }
    public FeeType Type { get; set; }
    public decimal Value { get; set; }
}
