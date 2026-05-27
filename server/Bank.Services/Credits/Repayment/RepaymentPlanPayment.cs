namespace Bank.Services.Credits;

public class RepaymentPlanPayment
{
    public int PaymentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal PrincipalPart { get; set; }
    public decimal InterestPart { get; set; }
    public decimal RemainingPrincipalAfterPayment { get; set; }
}
