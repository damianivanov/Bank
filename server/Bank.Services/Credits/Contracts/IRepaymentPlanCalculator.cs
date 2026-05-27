namespace Bank.Services.Credits;

public interface IRepaymentPlanCalculator
{
    RepaymentPlanCalculationResult Calculate(decimal principal, decimal annualInterestRate, int termMonths, DateTime grantedAtUtc);
}
