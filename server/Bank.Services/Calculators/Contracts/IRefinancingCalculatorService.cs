using Bank.Core.JsonModels.Calculators;

namespace Bank.Services.Calculators;

public interface IRefinancingCalculatorService : ICalculator<RefinancingCalculatorRequest, RefinancingCalculatorResponse>
{
}
