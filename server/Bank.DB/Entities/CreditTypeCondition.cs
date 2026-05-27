using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class CreditTypeCondition : BaseTrackUserEntity
{
    public CreditType CreditType { get; set; }
    public string Name { get; set; } = string.Empty;

    [Precision(9, 4)]
    public decimal StandardAnnualInterestRate { get; set; }

    [Precision(9, 4)]
    public decimal VipAnnualInterestRate { get; set; }

    [Precision(18, 2)]
    public decimal MaximumAmount { get; set; }
    public int MaximumTermMonths { get; set; }

    [Precision(18, 2)]
    public decimal StandardGrantingFee { get; set; }

    [Precision(18, 2)]
    public decimal VipGrantingFee { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Credit> Credits { get; set; } = [];
}
