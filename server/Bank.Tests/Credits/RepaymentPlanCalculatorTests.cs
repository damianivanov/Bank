using Bank.Core.Exceptions;
using Bank.Services.Credits;
using FluentAssertions;

namespace Bank.Tests.Credits;

public class RepaymentPlanCalculatorTests
{
    private readonly RepaymentPlanCalculator _calculator = new();

    private static readonly DateTime GrantedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Calculate_WithStandardLoan_ProducesOnePaymentPerMonth()
    {
        var result = _calculator.Calculate(10000m, 8.5m, 24, GrantedAt);

        result.Payments.Should().HaveCount(24);
        result.Payments.Select(p => p.PaymentNumber)
            .Should().BeInAscendingOrder()
            .And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void Calculate_WithKnownLoan_ProducesExpectedAnnuityPayment()
    {
        var result = _calculator.Calculate(10000m, 10m, 12, GrantedAt);

        result.PlannedMonthlyPaymentAmount.Should().Be(879.16m);
    }

    [Fact]
    public void Calculate_WithKnownLoan_SplitsFirstInstallmentBetweenInterestAndPrincipal()
    {
        var result = _calculator.Calculate(10000m, 10m, 12, GrantedAt);
        var firstPayment = result.Payments.Single(p => p.PaymentNumber == 1);

        firstPayment.InterestPart.Should().Be(83.33m);
        firstPayment.PrincipalPart.Should().Be(795.83m);
        firstPayment.PaymentAmount.Should().Be(879.16m);
        firstPayment.RemainingPrincipalAfterPayment.Should().Be(9204.17m);
    }

    [Fact]
    public void Calculate_WithAnnuity_KeepsEveryInstallmentEqualExceptTheLast()
    {
        var result = _calculator.Calculate(50000m, 8.5m, 84, GrantedAt);
        var ordered = result.Payments.OrderBy(p => p.PaymentNumber).ToList();

        ordered.Take(ordered.Count - 1)
            .Should().OnlyContain(p => p.PaymentAmount == result.PlannedMonthlyPaymentAmount);
    }

    [Fact]
    public void Calculate_WithAnnuity_ShiftsFromInterestTowardsPrincipalOverTime()
    {
        var ordered = _calculator.Calculate(50000m, 8.5m, 84, GrantedAt)
            .Payments.OrderBy(p => p.PaymentNumber).ToList();

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            ordered[i + 1].InterestPart.Should().BeLessThanOrEqualTo(ordered[i].InterestPart);
        }

        for (var i = 0; i < ordered.Count - 2; i++)
        {
            ordered[i + 1].PrincipalPart.Should().BeGreaterThanOrEqualTo(ordered[i].PrincipalPart);
        }
    }

    [Fact]
    public void Calculate_PrincipalPartsSumToOriginalPrincipal()
    {
        var principal = 50000m;

        var result = _calculator.Calculate(principal, 7.2m, 84, GrantedAt);

        result.Payments.Sum(p => p.PrincipalPart).Should().Be(principal);
    }

    [Fact]
    public void Calculate_LastPaymentLeavesZeroRemainingPrincipal()
    {
        var result = _calculator.Calculate(250000m, 3.9m, 360, GrantedAt);

        result.Payments.OrderBy(p => p.PaymentNumber).Last()
            .RemainingPrincipalAfterPayment.Should().Be(0m);
    }

    [Fact]
    public void Calculate_SchedulesDueDatesOneMonthApartStartingAfterGrant()
    {
        var result = _calculator.Calculate(10000m, 5m, 6, GrantedAt);

        result.Payments.OrderBy(p => p.PaymentNumber)
            .Select(p => p.DueDate)
            .Should().Equal(
                Enumerable.Range(1, 6).Select(m => GrantedAt.Date.AddMonths(m)));
    }

    [Fact]
    public void Calculate_WithZeroInterest_ProducesZeroInterestParts()
    {
        var result = _calculator.Calculate(1200m, 0m, 12, GrantedAt);

        result.Payments.Should().OnlyContain(p => p.InterestPart == 0m);
        result.PlannedMonthlyPaymentAmount.Should().Be(100m);
    }

    [Theory]
    [InlineData(0, 2, 12)]
    [InlineData(-100, 2, 12)]
    [InlineData(1000, -1, 12)]
    [InlineData(1000, 2, 0)]
    [InlineData(1000, 2, -6)]
    public void Calculate_WithInvalidArguments_Throws(decimal principal, decimal annualRate, int termMonths)
    {
        var act = () => _calculator.Calculate(principal, annualRate, termMonths, GrantedAt);

        act.Should().Throw<BankException>();
    }
}
