using Bank.Core.Enums;
using Bank.Core.JsonModels.Calculators;

namespace Bank.Services.Calculators;

internal static class FeeMath
{
    public static decimal Resolve(Fee? fee, decimal baseAmount)
    {
        if (fee is null)
        {
            return 0m;
        }

        return fee.Type == FeeType.Percent
            ? baseAmount * fee.Value / 100m
            : fee.Value;
    }
}
