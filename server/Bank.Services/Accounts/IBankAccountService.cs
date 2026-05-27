using Bank.Core.JsonModels.Bank.Accounts;

namespace Bank.Services.Accounts;

public interface IBankAccountService
{
    Task<IReadOnlyCollection<BankAccountModel>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<BankAccountDetailsModel> GetAccountAsync(long accountId, CancellationToken cancellationToken = default);
    Task<BankAccountDetailsModel> CreateAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDetailsModel> CloseAccountAsync(long accountId, CancellationToken cancellationToken = default);
}
