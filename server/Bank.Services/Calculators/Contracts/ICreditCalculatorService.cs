using Bank.Core.JsonModels.Calculators;

namespace Bank.Services.Calculators;

public interface ICreditCalculatorService : ICalculator<CreditCalculatorRequest, CreditCalculatorResponse>
{
}
