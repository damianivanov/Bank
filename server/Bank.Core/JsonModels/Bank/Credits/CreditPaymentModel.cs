using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreditPaymentModel
{
    public long Id { get; set; }
    public int PaymentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal PrincipalPart { get; set; }
    public decimal InterestPart { get; set; }
    public decimal RemainingPrincipalAfterPayment { get; set; }
    public decimal FeePart { get; set; }
    public CreditPaymentStatus Status { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
