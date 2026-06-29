using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class CreditTermsFee : BaseTrackUserEntity
{
    public long CreditTermsId { get; set; }
    public CreditFeeKind Kind { get; set; }
    public FeeType Type { get; set; }

    [Precision(18, 2)]
    public decimal Value { get; set; }

    public CreditTerms CreditTerms { get; set; } = null!;
}
