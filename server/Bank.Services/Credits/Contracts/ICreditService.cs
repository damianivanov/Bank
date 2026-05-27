using Bank.Core.JsonModels.Bank.Credits;

namespace Bank.Services.Credits;

public interface ICreditService
{
    Task<IReadOnlyCollection<CreditModel>> GetCreditsAsync(CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> GetCreditAsync(long creditId, CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> CreateCreditAsync(CreateCreditRequest request, CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> PayPaymentAsync(long creditId, long paymentId, CancellationToken cancellationToken = default);
}
