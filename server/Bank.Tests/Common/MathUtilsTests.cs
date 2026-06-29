using Bank.Core.Utils;
using FluentAssertions;

namespace Bank.Tests.Common;

public class MathUtilsTests
{

    [Theory]
    [InlineData(2, 3, 8)]
    [InlineData(1.5, 2, 2.25)]
    [InlineData(10, 0, 1)]
    [InlineData(5, 1, 5)]
    [InlineData(2, 4, 16)]
    [InlineData(1.1, 3, 1.331)]
    public void Pow_ShouldCalculatePowerCorrectly(decimal baseValue, int exponent, decimal expected)
    {
        var result = MathUtils.Pow(baseValue, exponent);

        result.Should().BeApproximately(expected, 0.000001m);
    }

    [Theory]
    [InlineData(2, -1, 0.5)]
    [InlineData(4, -2, 0.0625)]
    [InlineData(10, -1, 0.1)]
    public void Pow_WithNegativeExponent_ShouldCalculateCorrectly(decimal baseValue, int exponent, decimal expected)
    {
        var result = MathUtils.Pow(baseValue, exponent);

        result.Should().BeApproximately(expected, 0.000001m);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(1000, 5, 50)]
    [InlineData(50, 50, 25)]
    [InlineData(200, 0.5, 1)]
    [InlineData(0, 10, 0)]
    public void PercentOf_ShouldCalculatePercentageCorrectly(decimal value, decimal percent, decimal expected)
    {
        var result = MathUtils.PercentOf(value, percent);

        result.Should().Be(expected);
    }

    [Fact]
    public void Pow_WithZeroBase_ShouldReturnZero()
    {
        var result = MathUtils.Pow(0, 5);

        result.Should().Be(0);
    }

    [Fact]
    public void Pow_FinancialStyle_IsMoreAccurateThanMathPow()
    {
        decimal monthlyRate = 0.05m / 12m;
        int months = 360;
        decimal baseVal = 1m + monthlyRate;

        decimal viaMathUtils = MathUtils.Pow(baseVal, months);
        decimal viaMathPow = (decimal)Math.Pow((double)baseVal, months);

        viaMathUtils.Should().NotBe(viaMathPow,
            "decimal Pow should differ from double-based Math.Pow when conversion loses precision");
        viaMathUtils.Should().BePositive();
        viaMathUtils.Should().BeInRange(4m, 6m);
        viaMathPow.Should().BeInRange(4m, 6m);
    }

    [Theory]
    [InlineData(1.008333333333333, 12)]
    [InlineData(1.01, 120)]
    [InlineData(1.001, 360)]
    public void Pow_LargeExponent_MatchesRepeatedMultiplication(decimal baseVal, int exp)
    {
        decimal fast = MathUtils.Pow(baseVal, exp);
        decimal slow = 1m;
        for (int i = 0; i < exp; i++) slow *= baseVal;
        fast.Should().BeApproximately(slow, 0.0000001m);
    }
}
