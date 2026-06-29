using Bank.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Calculators;

public class RefinancingCalculatorRequest
{
    [Required]
    public CurrentLoanInput CurrentLoan { get; set; } = new();

    [Required]
    public NewLoanInput NewLoan { get; set; } = new();
}

public class CurrentLoanInput
{
    [Range(CalculatorLimits.MinAmount, CalculatorLimits.MaxAmount)]
    public decimal Principal { get; set; }

    [Range(CalculatorLimits.MinRate, CalculatorLimits.MaxRate)]
    public decimal AnnualRatePercent { get; set; }

    [Range(CalculatorLimits.MinTermMonths, CalculatorLimits.MaxTermMonths)]
    public int TermMonths { get; set; }

    [Range(0, CalculatorLimits.MaxTermMonths)]
    public int PaymentsMade { get; set; }

    [Range(CalculatorLimits.MinPercent, CalculatorLimits.MaxPercent)]
    public decimal PrepaymentFeePercent { get; set; }
}

public class NewLoanInput
{
    [Range(CalculatorLimits.MinRate, CalculatorLimits.MaxRate)]
    public decimal AnnualRatePercent { get; set; }

    [Range(CalculatorLimits.MinPercent, CalculatorLimits.MaxPercent)]
    public decimal OriginationFeePercent { get; set; }

    [Range(CalculatorLimits.MinNonNegativeAmount, CalculatorLimits.MaxAmount)]
    public decimal OriginationFeeFixed { get; set; }
}

public class RefinancingCalculatorResponse
{
    public int RemainingMonths { get; set; }

    public decimal RemainingPrincipal { get; set; }

    public LoanSideResult Current { get; set; } = new();

    public LoanSideResult New { get; set; } = new();

    public decimal Savings { get; set; }

    public bool ShouldYouSwitch { get; set; }
}

public class LoanSideResult
{
    public decimal AnnualRatePercent { get; set; }

    public int TermMonths { get; set; }

    public decimal MonthlyPayment { get; set; }

    public decimal Fees { get; set; }

    public decimal TotalToPay { get; set; }
}
