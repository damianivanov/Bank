using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.Accounts;
using Bank.DB.Constants;
using Bank.Services.Accounts;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Roles = RoleNames.StaffOrAdmin)]
[Route("api/accounts")]
public class AccountsController : BaseApiController
{
    private readonly IBankAccountService bankAccountService;

    public AccountsController(IBankAccountService bankAccountService)
    {
        this.bankAccountService = bankAccountService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<BankAccountModel>>>> GetAccounts(CancellationToken cancellationToken)
    {
        var accounts = await bankAccountService.GetAccountsAsync(cancellationToken);
        return this.ReturnJson(accounts);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<BankAccountDetailsModel>>> GetAccount(long id, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.GetAccountAsync(id, cancellationToken);
        return this.ReturnJson(account);
    }

    [HttpPost]
    public async Task<ActionResult<CommonJsonModel<BankAccountDetailsModel>>> CreateAccount(CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.CreateAccountAsync(request, cancellationToken);
        return this.ReturnJson(account);
    }

    [HttpPut("{id:long}/close")]
    public async Task<ActionResult<CommonJsonModel<BankAccountDetailsModel>>> CloseAccount(long id, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.CloseAccountAsync(id, cancellationToken);
        return this.ReturnJson(account);
    }
}
