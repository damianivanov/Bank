using Bank.Core.Enums;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using FluentAssertions;

namespace Bank.Tests.Calculators;

public class FinanceHelpersTests
{
    [Theory]
    [InlineData(12, 0.01)]
    [InlineData(0, 0)]
    [InlineData(2.4, 0.002)]
    public void RateMath_MonthlyFromAnnualPercent_DividesByHundredThenTwelve(decimal annualPercent, decimal expected)
    {
        RateMath.MonthlyFromAnnualPercent(annualPercent)
            .Should().Be(annualPercent / 100m / 12m)
            .And.BeApproximately(expected, 0.0000001m);
    }

    [Fact]
    public void RateMath_EffectiveAnnualPercentFromMonthly_CompoundsTwelveMonths()
    {
        var monthlyRate = 0.01m;

        RateMath.EffectiveAnnualPercentFromMonthly(monthlyRate)
            .Should().BeApproximately(12.6825030131969720661201m, 0.0001m);
    }

    [Fact]
    public void RateMath_EffectiveAnnualPercentFromMonthly_ZeroMonthlyRateIsZero()
    {
        RateMath.EffectiveAnnualPercentFromMonthly(0m).Should().Be(0m);
    }

    [Fact]
    public void FeeMath_Resolve_NullFee_IsZero()
    {
        FeeMath.Resolve(null, 1000m).Should().Be(0m);
    }

    [Fact]
    public void FeeMath_Resolve_PercentFee_IsPercentageOfBase()
    {
        var fee = new Fee { Type = FeeType.Percent, Value = 2m };

        FeeMath.Resolve(fee, 1000m).Should().Be(20m);
    }

    [Fact]
    public void FeeMath_Resolve_CurrencyFee_IsFlatValueIgnoringBase()
    {
        var fee = new Fee { Type = FeeType.Currency, Value = 150m };

        FeeMath.Resolve(fee, 1000m).Should().Be(150m);
    }
}
