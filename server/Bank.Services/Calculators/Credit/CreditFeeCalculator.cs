using Bank.Core.JsonModels.Calculators;

namespace Bank.Services.Calculators;

internal static class CreditFeeCalculator
{
    public static decimal InitialFees(CreditCalculatorRequest request)
    {
        return FeeMath.Resolve(request.ApplicationFee, request.LoanAmount)
             + FeeMath.Resolve(request.ProcessingFee, request.LoanAmount)
             + FeeMath.Resolve(request.OtherInitialFees, request.LoanAmount);
    }

    public static decimal MonthlyFees(CreditCalculatorRequest request, decimal remainingBalance)
    {
        return FeeMath.Resolve(request.MonthlyManagementFee, remainingBalance)
             + FeeMath.Resolve(request.OtherMonthlyFees, remainingBalance);
    }

    public static decimal AnnualFeesForMonth(CreditCalculatorRequest request, int month)
    {
        if (month <= 1 || (month - 1) % 12 != 0)
        {
            return 0m;
        }

        return FeeMath.Resolve(request.AnnualManagementFee, request.LoanAmount)
             + FeeMath.Resolve(request.OtherAnnualFees, request.LoanAmount);
    }
}
