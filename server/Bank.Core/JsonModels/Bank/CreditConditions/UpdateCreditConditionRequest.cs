using System.ComponentModel.DataAnnotations;
using Bank.Core.Validation;

namespace Bank.Core.JsonModels.Bank.CreditConditions;

public class UpdateCreditConditionRequest
{
    [Range(0, 100)]
    public decimal StandardAnnualInterestRate { get; set; }

    [Range(0, 100)]
    public decimal VipAnnualInterestRate { get; set; }

    [Range(0.01, 1_000_000_000)]
    public decimal MaximumAmount { get; set; }

    [Range(1, CalculatorLimits.MaxTermMonths)]
    public int MaximumTermMonths { get; set; }

    [Range(0, 1_000_000)]
    public decimal StandardGrantingFee { get; set; }

    [Range(0, 1_000_000)]
    public decimal VipGrantingFee { get; set; }
}
