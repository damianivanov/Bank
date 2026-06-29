using Bank.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Calculators;

public class LeasingCalculatorRequest
{
    [Range(CalculatorLimits.MinAmount, CalculatorLimits.MaxAmount)]
    public decimal PriceWithVAT { get; set; }

    [Range(CalculatorLimits.MinNonNegativeAmount, CalculatorLimits.MaxAmount)]
    public decimal DownPayment { get; set; }

    [Range(CalculatorLimits.MinTermMonths, CalculatorLimits.MaxTermMonths)]
    public int LeasingTerm { get; set; }

    [Range(CalculatorLimits.MinAmount, CalculatorLimits.MaxAmount)]
    public decimal MonthlyPayment { get; set; }

    public Fee? ProcessingFee { get; set; }
}

public class LeasingCalculatorResponse
{
    public decimal TotalCost { get; set; }
    public decimal TotalMarkup { get; set; }
    public decimal MarkupPercentage { get; set; }
    public decimal EffectiveInterestRate { get; set; }
    public decimal ProcessingFeeAmount { get; set; }
    public decimal TotalPaid { get; set; }
}
