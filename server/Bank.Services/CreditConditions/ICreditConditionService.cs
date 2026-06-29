using Bank.Core.JsonModels.Bank.CreditConditions;

namespace Bank.Services.CreditConditions;

public interface ICreditConditionService
{
    Task<IReadOnlyCollection<CreditTypeConditionModel>> GetCreditConditionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PublicCreditConditionModel>> GetPublicCreditConditionsAsync(CancellationToken cancellationToken = default);

    Task<CreditTypeConditionModel> UpdateCreditConditionAsync(long id, UpdateCreditConditionRequest request, CancellationToken cancellationToken = default);
}
