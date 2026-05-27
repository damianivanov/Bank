using Bank.Core.Exceptions;
using Bank.Services.Credits;

namespace Bank.Tests;

public class RepaymentPlanCalculatorTests
{
    private readonly RepaymentPlanCalculator calculator = new();

    [Fact]
    public void Calculate_ReturnsExpectedPaymentCount()
    {
        var result = calculator.Calculate(10000m, 8.5m, 24, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(24, result.Payments.Count);
    }

    [Fact]
    public void Calculate_WithZeroInterest_ProducesZeroInterestParts()
    {
        var result = calculator.Calculate(1200m, 0m, 12, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.All(result.Payments, payment => Assert.Equal(0m, payment.InterestPart));
    }

    [Fact]
    public void Calculate_PrincipalPartsSumToOriginalPrincipal()
    {
        var principal = 50000m;
        var result = calculator.Calculate(principal, 7.2m, 84, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var sumPrincipalParts = result.Payments.Sum(payment => payment.PrincipalPart);
        Assert.Equal(principal, sumPrincipalParts);
    }

    [Fact]
    public void Calculate_LastPaymentLeavesZeroRemainingPrincipal()
    {
        var result = calculator.Calculate(250000m, 3.9m, 360, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var lastPayment = result.Payments.OrderBy(payment => payment.PaymentNumber).Last();

        Assert.Equal(0m, lastPayment.RemainingPrincipalAfterPayment);
    }

    [Fact]
    public void Calculate_RejectsInvalidArguments()
    {
        Assert.Throws<BankException>(() => calculator.Calculate(0m, 2m, 12, DateTime.UtcNow));
        Assert.Throws<BankException>(() => calculator.Calculate(1000m, -1m, 12, DateTime.UtcNow));
        Assert.Throws<BankException>(() => calculator.Calculate(1000m, 2m, 0, DateTime.UtcNow));
    }
}
