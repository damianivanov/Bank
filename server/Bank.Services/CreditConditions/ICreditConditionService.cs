using Bank.Core.JsonModels.Bank.CreditConditions;

namespace Bank.Services.CreditConditions;

public interface ICreditConditionService
{
    Task<IReadOnlyCollection<CreditTypeConditionModel>> GetCreditConditionsAsync(CancellationToken cancellationToken = default);
}
