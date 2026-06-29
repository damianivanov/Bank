namespace Bank.Services.Calculators;

internal static class AnnuityRateSolver
{
    public static decimal EffectiveAnnualPercent(decimal netDisbursed, decimal monthlyPayment, int months)
    {
        if (netDisbursed <= 0m || months <= 0 || monthlyPayment <= 0m)
            return 0m;

        decimal low = 0m;
        decimal high = 1m;
        decimal target = netDisbursed;

        for (int iter = 0; iter < 200; iter++)
        {
            decimal mid = (low + high) / 2m;
            decimal pv = AnnuityMath.PresentValueOfAnnuity(monthlyPayment, mid, months);

            if (pv > target)
                low = mid;
            else
                high = mid;
        }

        decimal monthlyRate = (low + high) / 2m;

        return RateMath.EffectiveAnnualPercentFromMonthly(monthlyRate);
    }
}
