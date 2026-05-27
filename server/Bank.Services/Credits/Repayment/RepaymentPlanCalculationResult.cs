namespace Bank.Services.Credits;

public class RepaymentPlanCalculationResult
{
    public decimal PlannedMonthlyPaymentAmount { get; set; }
    public IReadOnlyCollection<RepaymentPlanPayment> Payments { get; set; } = [];
}
