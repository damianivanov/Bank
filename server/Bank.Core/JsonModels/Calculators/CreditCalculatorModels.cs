using Bank.Core.Enums;
using Bank.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Calculators;

public class CreditCalculatorRequest
{
    [Range(CalculatorLimits.MinAmount, CalculatorLimits.MaxAmount)]
    public decimal LoanAmount { get; set; }

    [Range(CalculatorLimits.MinTermMonths, CalculatorLimits.MaxTermMonths)]
    public int TermInMonths { get; set; }

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

public class CreditCalculatorResponse
{
    public decimal APR { get; set; }

    public decimal AverageMonthlyPayment { get; set; }

    public decimal TotalAmountWithFees { get; set; }

    public decimal TotalFees { get; set; }

    public decimal TotalInterest { get; set; }

    public decimal TotalPayments { get; set; }

    public List<PaymentScheduleItem> PaymentSchedule { get; set; } = new();
}

public class PaymentScheduleItem
{
    public int Month { get; set; }

    public DateTime Date { get; set; }

    public decimal Payment { get; set; }

    public decimal Principal { get; set; }

    public decimal Interest { get; set; }

    public decimal RemainingBalance { get; set; }

    public decimal Fees { get; set; }

    public decimal CashFlow { get; set; }
}
