using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class CreditInstallment : BaseTrackUserEntity
{
    public long CreditId { get; set; }
    public Credit Credit { get; set; } = null!;
    
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }

    [Precision(18, 2)]
    public decimal InstallmentAmount { get; set; }

    [Precision(18, 2)]
    public decimal PrincipalPart { get; set; }

    [Precision(18, 2)]
    public decimal InterestPart { get; set; }

    [Precision(18, 2)]
    public decimal RemainingPrincipalAfterPayment { get; set; }

    [Precision(18, 2)]
    public decimal FeePart { get; set; }
    public CreditPaymentStatus Status { get; set; }
    public DateTime? PaidAtUtc { get; set; }

}
