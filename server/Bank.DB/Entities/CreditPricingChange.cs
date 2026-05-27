using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class CreditPricingChange : BaseTrackUserEntity
{
    public long CreditId { get; set; }
    public Credit Credit { get; set; } = null!;

    [Precision(9, 4)]
    public decimal PreviousAnnualInterestRate { get; set; }

    [Precision(9, 4)]
    public decimal NewAnnualInterestRate { get; set; }
    public int EffectiveFromPaymentNumber { get; set; }
    public PricingChangeReason Reason { get; set; }

}
