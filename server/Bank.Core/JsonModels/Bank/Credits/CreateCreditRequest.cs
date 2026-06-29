using Bank.Core.Enums;
using Bank.Core.JsonModels.Calculators;
using Bank.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreateCreditRequest
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [EnumDataType(typeof(CreditType))]
    public CreditType CreditType { get; set; }

    [Range(CalculatorLimits.MinAmount, CalculatorLimits.MaxAmount)]
    public decimal GrantedAmount { get; set; }

    [Range(CalculatorLimits.MinTermMonths, CalculatorLimits.MaxTermMonths)]
    public int TermMonths { get; set; }

    // Per-deal условия, попълнени предварително от продукта, но редактируеми от служителя.
    [Range(CalculatorLimits.MinRate, CalculatorLimits.MaxRate)]
    public decimal InterestRate { get; set; }

    [EnumDataType(typeof(PaymentType))]
    public PaymentType PaymentType { get; set; }

    [Range(0, CalculatorLimits.MaxTermMonths)]
    public int? PromoPeriod { get; set; }

    [Range(CalculatorLimits.MinRate, CalculatorLimits.MaxRate)]
    public decimal? PromoRate { get; set; }

    [Range(0, CalculatorLimits.MaxTermMonths)]
    public int? GracePeriod { get; set; }

    public Fee? ApplicationFee { get; set; }
    public Fee? ProcessingFee { get; set; }
    public Fee? OtherInitialFees { get; set; }
    public Fee? AnnualManagementFee { get; set; }
    public Fee? OtherAnnualFees { get; set; }
    public Fee? MonthlyManagementFee { get; set; }
    public Fee? OtherMonthlyFees { get; set; }
}
