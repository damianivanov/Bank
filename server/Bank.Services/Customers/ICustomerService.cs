using Bank.Core.JsonModels.Bank.Customers;

namespace Bank.Services.Customers;

public interface ICustomerService
{
    Task<IReadOnlyCollection<CustomerModel>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerLookupModel>> GetCustomerLookupAsync(CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> CreateCustomerForUserAsync(long userId, CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> UpdateCustomerAsync(long customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDetailsModel> UpdateVipAsync(long customerId, UpdateCustomerVipRequest request, CancellationToken cancellationToken = default);
}
