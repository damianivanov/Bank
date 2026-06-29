using Bank.Core.Enums;
using Bank.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Calculators;

public class Fee
{
    [EnumDataType(typeof(FeeType))]
    public FeeType Type { get; set; }

    [Range(CalculatorLimits.MinNonNegativeAmount, CalculatorLimits.MaxAmount)]
    public decimal Value { get; set; }
}
