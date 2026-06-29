using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.Core.Validation;

namespace Bank.Services.Calculators;

public class LeasingCalculatorService : ILeasingCalculatorService
{
    private const int MaxTermMonths = CalculatorLimits.MaxTermMonths;

    public async Task<LeasingCalculatorResponse> CalculateAsync(LeasingCalculatorRequest request)
    {
        if (request.PriceWithVAT <= 0 || request.LeasingTerm <= 0 || request.MonthlyPayment <= 0)
            throw new BankException("Невалидни параметри за лизинг.");

        if (request.LeasingTerm > MaxTermMonths)
            throw new BankException($"Срокът на лизинга не може да надвишава {MaxTermMonths} месеца.");

        if (request.DownPayment < 0 || request.DownPayment >= request.PriceWithVAT)
            throw new BankException("Невалидна първоначална вноска.");

        var response = new LeasingCalculatorResponse();

        decimal leasedAmount = request.PriceWithVAT - request.DownPayment;

        decimal processingFeeAmount = FeeMath.Resolve(request.ProcessingFee, request.PriceWithVAT);

        decimal totalMonthlyPayments = request.MonthlyPayment * request.LeasingTerm;

        decimal totalPaid = request.DownPayment + totalMonthlyPayments + processingFeeAmount;

        decimal netDisbursed = leasedAmount - processingFeeAmount;
        decimal effectiveAnnualRate = AnnuityRateSolver.EffectiveAnnualPercent(netDisbursed, request.MonthlyPayment, request.LeasingTerm);

        response.TotalCost = request.PriceWithVAT;
        response.ProcessingFeeAmount = Math.Round(processingFeeAmount, 2, MidpointRounding.AwayFromZero);
        response.TotalPaid = Math.Round(totalPaid, 2, MidpointRounding.AwayFromZero);
        response.EffectiveInterestRate = Math.Round(effectiveAnnualRate, 2, MidpointRounding.AwayFromZero);

        response.TotalMarkup = Math.Round(totalPaid - request.PriceWithVAT, 2, MidpointRounding.AwayFromZero);
        response.MarkupPercentage = request.PriceWithVAT > 0
            ? Math.Round((response.TotalMarkup / request.PriceWithVAT) * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return await Task.FromResult(response);
    }
}
