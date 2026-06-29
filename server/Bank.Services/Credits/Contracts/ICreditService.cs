using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Common;

namespace Bank.Services.Credits;

public interface ICreditService
{
    Task<PagedResponse<CreditModel>> GetCreditsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> GetCreditAsync(long creditId, CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> GetCreditForCustomerAsync(long customerId, long creditId, CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> GetCreditForCustomersAsync(IReadOnlyCollection<long> customerIds, long creditId, CancellationToken cancellationToken = default);
    Task<CreditDetailsModel> CreateCreditAsync(CreateCreditRequest request, CancellationToken cancellationToken = default);
}
