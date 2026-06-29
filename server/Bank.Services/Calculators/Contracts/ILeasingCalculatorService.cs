using Bank.Core.JsonModels.Calculators;

namespace Bank.Services.Calculators;

public interface ILeasingCalculatorService : ICalculator<LeasingCalculatorRequest, LeasingCalculatorResponse>
{
}
