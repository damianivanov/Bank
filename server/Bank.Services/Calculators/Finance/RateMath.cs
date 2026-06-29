using Bank.Core.Utils;

namespace Bank.Services.Calculators;

internal static class RateMath
{
    public static decimal MonthlyFromAnnualPercent(decimal annualPercent) => annualPercent / 100m / 12m;

    public static decimal EffectiveAnnualPercentFromMonthly(decimal monthlyRate)
        => (MathUtils.Pow(1m + monthlyRate, 12) - 1m) * 100m;
}
