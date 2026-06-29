using Bank.Core.Enums;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.JsonModels.Common;

namespace Bank.Services.Customers;

public interface ICustomerService
{
    Task<PagedResponse<CustomerModel>> GetCustomersAsync(PagedRequest request, CustomerType? customerType = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerLookupModel>> GetCustomerLookupAsync(string? search, CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default);
    Task<CustomerEditModel> GetCustomerForEditAsync(long customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<long>> GetAccessibleCustomerIdsAsync(long personId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerLookupModel>> GetAccessibleCustomersAsync(long personId, CancellationToken cancellationToken = default);
    Task<CustomerModel> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerModel> CreateCustomerForUserAsync(long userId, CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerModel> RegisterCounterCustomerAsync(RegisterCounterCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerModel> UpdateCustomerAsync(long customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> UpdateVipAsync(long customerId, UpdateCustomerVipRequest request, CancellationToken cancellationToken = default);
}
