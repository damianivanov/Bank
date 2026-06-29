using Bank.Core.JsonModels.Calculators;
using Bank.Core.Utils;

namespace Bank.Services.Calculators;

internal static class AprCalculator
{
    public static decimal EffectiveAnnualPercent(IReadOnlyList<PaymentScheduleItem> schedule)
    {
        var rate = 0.01m;
        const decimal tolerance = 0.0000001m;
        const int maxIterations = 200;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var npv = 0m;
            var npvDerivative = 0m;

            foreach (var item in schedule)
            {
                var period = item.Month;
                var cashFlow = item.CashFlow;
                var denominator = MathUtils.Pow(1m + rate, period);

                npv += cashFlow / denominator;
                npvDerivative -= period * cashFlow / (denominator * (1m + rate));
            }

            if (Math.Abs(npv) < tolerance || npvDerivative == 0m)
            {
                break;
            }

            rate -= npv / npvDerivative;

            if (rate < -0.5m) rate = -0.5m;
            if (rate > 0.5m) rate = 0.5m;
        }

        return RateMath.EffectiveAnnualPercentFromMonthly(rate);
    }
}
