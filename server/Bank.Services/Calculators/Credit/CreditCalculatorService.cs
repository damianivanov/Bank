using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.Core.Validation;

namespace Bank.Services.Calculators;

public class CreditCalculatorService : ICreditCalculatorService
{
    private const int MaxTermMonths = CalculatorLimits.MaxTermMonths;

    private readonly TimeProvider timeProvider;

    public CreditCalculatorService(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<CreditCalculatorResponse> CalculateAsync(CreditCalculatorRequest request)
    {
        Validate(request);

        var response = new CreditCalculatorResponse();
        var schedule = new List<PaymentScheduleItem>();

        decimal remainingBalance = request.LoanAmount;
        int totalMonths = request.TermInMonths;

        decimal baseMonthlyRate = RateMath.MonthlyFromAnnualPercent(request.InterestRate);

        decimal totalInitialFees = Math.Round(CreditFeeCalculator.InitialFees(request), 2, MidpointRounding.AwayFromZero);

        decimal totalInterestPaid = 0m;
        decimal totalPrincipalPaid = 0m;
        decimal totalFeesPaid = totalInitialFees;

        var paymentPlanner = CreatePlanner(request.PaymentType);

        DateTime startDate = DateTime.SpecifyKind(timeProvider.GetLocalNow().DateTime.Date, DateTimeKind.Local);

        schedule.Add(new PaymentScheduleItem
        {
            Month = 0,
            Date = startDate,
            Payment = 0,
            Principal = 0,
            Interest = 0,
            RemainingBalance = request.LoanAmount,
            Fees = totalInitialFees,
            CashFlow = request.LoanAmount - totalInitialFees
        });

        for (int month = 1; month <= totalMonths; month++)
        {
            decimal openingBalance = remainingBalance;

            decimal currentMonthlyRate = ResolveMonthlyRate(request, month, baseMonthlyRate, out bool isPromo);

            bool isGrace = request.GracePeriod.HasValue && month <= request.GracePeriod.Value;
            decimal interestPayment = Math.Round(remainingBalance * currentMonthlyRate, 2, MidpointRounding.AwayFromZero);
            decimal principalPayment = 0m;
            decimal monthlyPayment = 0m;

            if (isGrace)
            {
                monthlyPayment = interestPayment;
            }
            else
            {
                var split = paymentPlanner.Plan(month, totalMonths, remainingBalance, currentMonthlyRate, isPromo, interestPayment);
                monthlyPayment = split.Payment;
                principalPayment = split.Principal;
            }

            decimal monthlyFees = CreditFeeCalculator.MonthlyFees(request, remainingBalance);
            monthlyFees += CreditFeeCalculator.AnnualFeesForMonth(request, month);

            principalPayment = Math.Round(principalPayment, 2, MidpointRounding.AwayFromZero);
            interestPayment = Math.Round(interestPayment, 2, MidpointRounding.AwayFromZero);
            monthlyFees = Math.Round(monthlyFees, 2, MidpointRounding.AwayFromZero);

            totalInterestPaid += interestPayment;
            totalPrincipalPaid += principalPayment;
            totalFeesPaid += monthlyFees;

            remainingBalance -= principalPayment;

            decimal totalMonthlyPayment = monthlyPayment + monthlyFees;

            schedule.Add(new PaymentScheduleItem
            {
                Month = month,
                Date = startDate.AddMonths(month),
                Payment = monthlyPayment,
                Principal = principalPayment,
                Interest = interestPayment,
                RemainingBalance = openingBalance,
                Fees = monthlyFees,
                CashFlow = -totalMonthlyPayment
            });
        }

        response.PaymentSchedule = schedule;
        BuildSummary(response, schedule, totalPrincipalPaid, totalInterestPaid, totalFeesPaid);

        return await Task.FromResult(response);
    }

    private static void Validate(CreditCalculatorRequest request)
    {
        if (request.LoanAmount <= 0 || request.TermInMonths <= 0 || request.InterestRate < 0)
            throw new BankException("Невалидни параметри за кредит.");

        if (request.TermInMonths > MaxTermMonths)
            throw new BankException($"Срокът не може да надвишава {MaxTermMonths} месеца.");
    }

    private static decimal ResolveMonthlyRate(CreditCalculatorRequest request, int month, decimal baseMonthlyRate, out bool isPromo)
    {
        isPromo = request.PromoPeriod.HasValue
                  && request.PromoRate.HasValue
                  && month <= request.PromoPeriod.Value;

        return isPromo
            ? RateMath.MonthlyFromAnnualPercent(request.PromoRate!.Value)
            : baseMonthlyRate;
    }

    private static void BuildSummary(
        CreditCalculatorResponse response,
        List<PaymentScheduleItem> schedule,
        decimal totalPrincipalPaid,
        decimal totalInterestPaid,
        decimal totalFeesPaid)
    {
        response.AverageMonthlyPayment = schedule
            .Where(s => s.Month >= 1)
            .Average(s => s.Payment);

        response.TotalPayments = totalPrincipalPaid + totalInterestPaid;
        response.TotalFees = totalFeesPaid;
        response.TotalInterest = totalInterestPaid;
        response.TotalAmountWithFees = response.TotalPayments + response.TotalFees;

        response.APR = AprCalculator.EffectiveAnnualPercent(schedule);
    }

    private static IPaymentPlanner CreatePlanner(PaymentType paymentType)
        => paymentType == PaymentType.Annuity
            ? new AnnuityPaymentPlanner()
            : new DecliningPaymentPlanner();

    private readonly record struct PaymentSplit(decimal Payment, decimal Principal);

    private interface IPaymentPlanner
    {
        PaymentSplit Plan(int month, int totalMonths, decimal remainingBalance, decimal currentMonthlyRate, bool isPromo, decimal interestPayment);
    }

    private sealed class AnnuityPaymentPlanner : IPaymentPlanner
    {
        private decimal? fixedAnnuityPayment;

        public PaymentSplit Plan(int month, int totalMonths, decimal remainingBalance, decimal currentMonthlyRate, bool isPromo, decimal interestPayment)
        {
            decimal monthlyPayment;

            if (!fixedAnnuityPayment.HasValue)
            {
                int remainingMonths = totalMonths - month + 1;

                if (isPromo)
                {
                    monthlyPayment = AnnuityMath.MonthlyPayment(remainingBalance, currentMonthlyRate, remainingMonths);
                }
                else
                {
                    fixedAnnuityPayment = AnnuityMath.MonthlyPayment(remainingBalance, currentMonthlyRate, remainingMonths);

                    monthlyPayment = fixedAnnuityPayment.Value;
                }
            }
            else
            {
                monthlyPayment = fixedAnnuityPayment.Value;
            }

            monthlyPayment = Math.Round(monthlyPayment, 2, MidpointRounding.AwayFromZero);
            interestPayment = Math.Round(interestPayment, 2, MidpointRounding.AwayFromZero);

            decimal principalPayment = monthlyPayment - interestPayment;

            if (principalPayment > remainingBalance)
            {
                principalPayment = remainingBalance;
                monthlyPayment = principalPayment + interestPayment;
            }

            principalPayment = Math.Round(principalPayment, 2, MidpointRounding.AwayFromZero);

            if (month == totalMonths)
            {
                principalPayment = Math.Round(remainingBalance, 2, MidpointRounding.AwayFromZero);
                monthlyPayment = principalPayment + interestPayment;
            }

            return new PaymentSplit(monthlyPayment, principalPayment);
        }
    }

    private sealed class DecliningPaymentPlanner : IPaymentPlanner
    {
        public PaymentSplit Plan(int month, int totalMonths, decimal remainingBalance, decimal currentMonthlyRate, bool isPromo, decimal interestPayment)
        {
            int remainingMonths = totalMonths - month + 1;
            decimal principalPayment = Math.Round(remainingBalance / remainingMonths, 2, MidpointRounding.AwayFromZero);
            decimal monthlyPayment = principalPayment + interestPayment;

            return new PaymentSplit(monthlyPayment, principalPayment);
        }
    }
}
