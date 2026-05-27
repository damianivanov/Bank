namespace Bank.Services.Credits;

public interface ICreditRepricingService
{
    Task RepriceActiveCreditsForCustomerAsync(long customerId, CancellationToken cancellationToken = default);
}
