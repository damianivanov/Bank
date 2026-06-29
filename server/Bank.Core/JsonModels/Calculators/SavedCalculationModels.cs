using Bank.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Calculators;

// Трябва да се подаде точно един от типизираните input обекти, съответстващ на Type; останалите се игнорират.
public class SaveCalculationRequest
{
    [EnumDataType(typeof(CalculatorType))]
    public CalculatorType Type { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public CreditCalculatorRequest? Credit { get; set; }

    public LeasingCalculatorRequest? Leasing { get; set; }

    public RefinancingCalculatorRequest? Refinancing { get; set; }
}

// Метаданни за елемент от списъка със записани калкулации (без inputs, без преизчислен резултат).
public class SavedCalculationModel
{
    public long Id { get; set; }

    public CalculatorType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

// Записана калкулация със съхранените си inputs и наново преизчислен резултат; зададена е само двойката, съответстваща на Type.
public class SavedCalculationDetailsModel
{
    public long Id { get; set; }

    public CalculatorType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public CreditCalculatorRequest? CreditInputs { get; set; }

    public CreditCalculatorResponse? CreditResult { get; set; }

    public LeasingCalculatorRequest? LeasingInputs { get; set; }

    public LeasingCalculatorResponse? LeasingResult { get; set; }

    public RefinancingCalculatorRequest? RefinancingInputs { get; set; }

    public RefinancingCalculatorResponse? RefinancingResult { get; set; }
}
