using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.Core.Utils;

namespace Bank.Services.Calculators;

public class RefinancingCalculatorService : IRefinancingCalculatorService
{
    public async Task<RefinancingCalculatorResponse> CalculateAsync(RefinancingCalculatorRequest request)
    {
        if (request.CurrentLoan.TermMonths <= 0)
            throw new BankException("Текущият срок на кредита трябва да е по-голям от нула.");

        if (request.CurrentLoan.PaymentsMade < 0 || request.CurrentLoan.PaymentsMade > request.CurrentLoan.TermMonths)
            throw new BankException("Извършените плащания трябва да са между нула и текущия срок на кредита.");

        int remainingMonths = request.CurrentLoan.TermMonths - request.CurrentLoan.PaymentsMade;

        decimal currentMonthlyRate = RateMath.MonthlyFromAnnualPercent(request.CurrentLoan.AnnualRatePercent);

        decimal currentMonthlyPaymentRaw = AnnuityMath.MonthlyPayment(
            request.CurrentLoan.Principal,
            currentMonthlyRate,
            request.CurrentLoan.TermMonths);

        decimal currentMonthlyPaymentDisplay = Math.Round(currentMonthlyPaymentRaw, 2, MidpointRounding.AwayFromZero);

        decimal remainingPrincipalRaw = AnnuityMath.RemainingBalance(
            request.CurrentLoan.Principal,
            currentMonthlyRate,
            request.CurrentLoan.TermMonths,
            request.CurrentLoan.PaymentsMade);

        decimal remainingPrincipalDisplay = Math.Round(remainingPrincipalRaw, 2, MidpointRounding.AwayFromZero);

        decimal currentTotalRemainingRaw = currentMonthlyPaymentRaw * remainingMonths;

        decimal prepaymentFeeRaw = MathUtils.PercentOf(remainingPrincipalRaw, request.CurrentLoan.PrepaymentFeePercent);
        decimal prepaymentFeeDisplay = Math.Round(prepaymentFeeRaw, 2, MidpointRounding.AwayFromZero);

        int newTermMonths = remainingMonths;

        decimal originationFeePercentAmountRaw =
            MathUtils.PercentOf(request.CurrentLoan.Principal, request.NewLoan.OriginationFeePercent);

        decimal originationFeeFixedRaw = request.NewLoan.OriginationFeeFixed;

        decimal baseNewPrincipalRaw = remainingPrincipalRaw;

        decimal newMonthlyPaymentRaw = newTermMonths == 0
            ? 0m
            : AnnuityMath.MonthlyPayment(baseNewPrincipalRaw, RateMath.MonthlyFromAnnualPercent(request.NewLoan.AnnualRatePercent), newTermMonths);

        decimal newMonthlyPaymentDisplay = Math.Round(newMonthlyPaymentRaw, 2, MidpointRounding.AwayFromZero);

        decimal newTotalRemainingPaidRaw = newMonthlyPaymentRaw * newTermMonths;

        decimal currentTotalRaw = currentTotalRemainingRaw;
        decimal currentTotalDisplay = Math.Round(currentTotalRaw, 2, MidpointRounding.AwayFromZero);

        decimal newTotalRaw = newTotalRemainingPaidRaw
                              + prepaymentFeeRaw
                              + originationFeePercentAmountRaw
                              + originationFeeFixedRaw;

        decimal newTotalDisplay = Math.Round(newTotalRaw, 2, MidpointRounding.AwayFromZero);

        decimal savingsRaw = currentTotalRaw - newTotalRaw;
        decimal savingsDisplay = Math.Round(savingsRaw, 2, MidpointRounding.AwayFromZero);

        var response = new RefinancingCalculatorResponse
        {
            RemainingMonths = remainingMonths,
            RemainingPrincipal = remainingPrincipalDisplay,

            Current = new LoanSideResult
            {
                AnnualRatePercent = request.CurrentLoan.AnnualRatePercent,
                TermMonths = request.CurrentLoan.TermMonths,
                MonthlyPayment = currentMonthlyPaymentDisplay,
                Fees = prepaymentFeeDisplay,
                TotalToPay = currentTotalDisplay
            },

            New = new LoanSideResult
            {
                AnnualRatePercent = request.NewLoan.AnnualRatePercent,
                TermMonths = newTermMonths,
                MonthlyPayment = newMonthlyPaymentDisplay,
                Fees = 0m,
                TotalToPay = newTotalDisplay
            },

            Savings = savingsDisplay,
            ShouldYouSwitch = savingsRaw > 0m
        };

        return await Task.FromResult(response);
    }
}
