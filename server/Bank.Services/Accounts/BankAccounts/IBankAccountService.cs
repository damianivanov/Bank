using Bank.Core.JsonModels.Bank.Accounts;
using Bank.Core.JsonModels.Common;

namespace Bank.Services.Accounts.BankAccounts;

public interface IBankAccountService
{
    Task<PagedResponse<BankAccountModel>> GetAccountsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDetailsModel> GetAccountAsync(long accountId, CancellationToken cancellationToken = default);
    Task<BankAccountDetailsModel> CreateAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDetailsModel> CloseAccountAsync(long accountId, CancellationToken cancellationToken = default);
}
