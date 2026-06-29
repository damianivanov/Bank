using Bank.Core.JsonModels.Calculators;

namespace Bank.Services.Calculators;

public interface ISavedCalculationService
{
    Task<SavedCalculationModel> SaveAsync(long userId, SaveCalculationRequest request, CancellationToken cancellationToken = default);

    Task<SavedCalculationModel> UpdateAsync(long userId, long id, SaveCalculationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SavedCalculationModel>> ListAsync(long userId, CancellationToken cancellationToken = default);

    Task<SavedCalculationDetailsModel> GetAsync(long userId, long id, CancellationToken cancellationToken = default);

    Task DeleteAsync(long userId, long id, CancellationToken cancellationToken = default);
}
