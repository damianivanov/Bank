using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class Credit : BaseTrackUserEntity
{
    public long CustomerId { get; set; }
    public long CreditTypeConditionId { get; set; }

    [Precision(18, 2)]
    public decimal GrantedAmount { get; set; }

    public int TermMonths { get; set; }

    [Precision(9, 4)]
    public decimal AppliedAnnualInterestRate { get; set; }

    [Precision(18, 2)]
    public decimal AppliedGrantingFee { get; set; }

    public bool CustomerWasVipAtCreation { get; set; }

    [Precision(18, 2)]
    public decimal PlannedMonthlyPaymentAmount { get; set; }
    public CreditStatus Status { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public DateTime? RepaidAtUtc { get; set; }

    public Customer Customer { get; set; } = null!;
    public CreditTypeCondition CreditTypeCondition { get; set; } = null!;
    public ICollection<CreditInstallment> Installments { get; set; } = [];
    public ICollection<CreditPricingChange> PricingChanges { get; set; } = [];
    public ICollection<CreditTerms> Terms { get; set; } = [];
}
