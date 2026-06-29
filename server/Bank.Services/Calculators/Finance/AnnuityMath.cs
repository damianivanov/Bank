using Bank.Core.Utils;

namespace Bank.Services.Calculators;

internal static class AnnuityMath
{
    public static decimal MonthlyPayment(decimal principal, decimal monthlyRate, int months)
    {
        if (months <= 0)
        {
            return 0m;
        }

        if (monthlyRate == 0m)
        {
            return principal / months;
        }

        var pow = MathUtils.Pow(1m + monthlyRate, months);
        return principal * (monthlyRate * pow) / (pow - 1m);
    }

    public static decimal PresentValueOfAnnuity(decimal payment, decimal monthlyRate, int months)
    {
        if (months <= 0)
        {
            return 0m;
        }

        if (monthlyRate == 0m)
        {
            return payment * months;
        }

        var discountFactor = 1m / (1m + monthlyRate);
        var presentValue = 0m;
        var power = discountFactor;

        for (var period = 1; period <= months; period++)
        {
            presentValue += payment * power;
            power *= discountFactor;
        }

        return presentValue;
    }

    public static decimal RemainingBalance(decimal principal, decimal monthlyRate, int months, int paymentsMade)
    {
        if (paymentsMade <= 0)
        {
            return principal;
        }

        if (paymentsMade >= months)
        {
            return 0m;
        }

        if (monthlyRate == 0m)
        {
            var paidPrincipal = principal / months * paymentsMade;
            return Math.Max(0m, principal - paidPrincipal);
        }

        var payment = MonthlyPayment(principal, monthlyRate, months);
        var powK = MathUtils.Pow(1m + monthlyRate, paymentsMade);
        var balance = principal * powK - payment * (powK - 1m) / monthlyRate;

        return balance < 0m ? 0m : balance;
    }
}
