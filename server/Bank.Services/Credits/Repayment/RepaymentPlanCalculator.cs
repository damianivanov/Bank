using Bank.Core.Exceptions;

namespace Bank.Services.Credits;

public class RepaymentPlanCalculator : IRepaymentPlanCalculator
{
    public RepaymentPlanCalculationResult Calculate(decimal principal, decimal annualInterestRate, int termMonths, DateTime grantedAtUtc)
    {
        if (principal <= 0m)
        {
            throw new BankException("Главницата по кредита трябва да е по-голяма от нула.");
        }

        if (annualInterestRate < 0m)
        {
            throw new BankException("Годишният лихвен процент не може да е отрицателен.");
        }

        if (termMonths <= 0)
        {
            throw new BankException("Срокът на кредита трябва да е по-голям от нула.");
        }

        var monthlyRate = annualInterestRate / 100m / 12m;
        var plannedMonthlyPayment = CalculateAnnuityPayment(principal, monthlyRate, termMonths);
        var roundedPlannedMonthlyPayment = decimal.Round(plannedMonthlyPayment, 2, MidpointRounding.AwayFromZero);

        var payments = new List<RepaymentPlanPayment>(termMonths);
        var remainingPrincipal = principal;

        for (var paymentNumber = 1; paymentNumber <= termMonths; paymentNumber++)
        {
            var dueDate = DateTime.SpecifyKind(grantedAtUtc, DateTimeKind.Utc).Date.AddMonths(paymentNumber);
            var interestPart = decimal.Round(remainingPrincipal * monthlyRate, 2, MidpointRounding.AwayFromZero);

            decimal paymentAmount;
            decimal principalPart;
            decimal remainingPrincipalAfterPayment;

            if (paymentNumber < termMonths)
            {
                paymentAmount = roundedPlannedMonthlyPayment;
                principalPart = decimal.Round(paymentAmount - interestPart, 2, MidpointRounding.AwayFromZero);

                if (principalPart > remainingPrincipal)
                {
                    principalPart = remainingPrincipal;
                }

                remainingPrincipalAfterPayment = decimal.Round(remainingPrincipal - principalPart, 2, MidpointRounding.AwayFromZero);
                if (remainingPrincipalAfterPayment < 0m)
                {
                    remainingPrincipalAfterPayment = 0m;
                }
            }
            else
            {
                principalPart = remainingPrincipal;
                paymentAmount = principalPart + interestPart;
                remainingPrincipalAfterPayment = 0m;
            }

            payments.Add(new RepaymentPlanPayment
            {
                PaymentNumber = paymentNumber,
                DueDate = dueDate,
                PaymentAmount = paymentAmount,
                PrincipalPart = principalPart,
                InterestPart = interestPart,
                RemainingPrincipalAfterPayment = remainingPrincipalAfterPayment,
            });

            remainingPrincipal = remainingPrincipalAfterPayment;
        }

        return new RepaymentPlanCalculationResult
        {
            PlannedMonthlyPaymentAmount = roundedPlannedMonthlyPayment,
            Payments = payments,
        };
    }

    private static decimal CalculateAnnuityPayment(decimal principal, decimal monthlyRate, int termMonths)
    {
        if (monthlyRate == 0m)
        {
            return principal / termMonths;
        }

        var ratePower = MathUtils.Pow(1m + monthlyRate, termMonths);
        var numerator = principal * (monthlyRate * ratePower);
        var denominator = ratePower - 1m;
        return numerator / denominator;
    }
}
