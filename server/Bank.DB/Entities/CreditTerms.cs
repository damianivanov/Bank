using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class CreditTerms : BaseTrackUserEntity
{
    public long CreditId { get; set; }
    public bool IsCurrent { get; set; }
    public int EffectiveFromPaymentNumber { get; set; }
    public CreditTermsOrigin Origin { get; set; }
    public PaymentType PaymentType { get; set; }

    [Precision(9, 4)]
    public decimal BaseAnnualInterestRate { get; set; }

    public int PromoPeriodMonths { get; set; }

    [Precision(9, 4)]
    public decimal? PromoAnnualInterestRate { get; set; }

    public int GracePeriodMonths { get; set; }

    [Precision(9, 4)]
    public decimal Apr { get; set; }

    public bool WasVipApplied { get; set; }

    [Precision(18, 2)]
    public decimal PlannedMonthlyPaymentAmount { get; set; }

    public Credit Credit { get; set; } = null!;
    public ICollection<CreditTermsFee> Fees { get; set; } = [];
}
