using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class CreditPayment : BaseTrackUserEntity
{
    public long CreditId { get; set; }
    public int PaymentNumber { get; set; }
    public DateTime DueDate { get; set; }

    [Precision(18, 2)]
    public decimal PaymentAmount { get; set; }

    [Precision(18, 2)]
    public decimal PrincipalPart { get; set; }

    [Precision(18, 2)]
    public decimal InterestPart { get; set; }

    [Precision(18, 2)]
    public decimal RemainingPrincipalAfterPayment { get; set; }
    public CreditPaymentStatus Status { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public Credit Credit { get; set; } = null!;
}
