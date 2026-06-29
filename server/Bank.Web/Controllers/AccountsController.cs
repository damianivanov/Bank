using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.Accounts;
using Bank.Core.JsonModels.Common;
using Bank.Services.Accounts.BankAccounts;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

// Това е контролер за управление на банкови сметки. 
// Той предоставя действия за извличане, създаване и затваряне на банкови сметки на клиенти.
[Authorize(Policy = Policies.RequireStaff)]
[Route("api/accounts")]
public class AccountsController : BaseApiController
{
    private readonly IBankAccountService bankAccountService;

    public AccountsController(IBankAccountService bankAccountService)
    {
        this.bankAccountService = bankAccountService;
    }

    [HttpGet] // Взимаме списък с всички банкови сметки, с възможност за pagination.
    public async Task<ActionResult<CommonJsonModel<PagedResponse<BankAccountModel>>>> GetAccounts([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var accounts = await bankAccountService.GetAccountsAsync(request, cancellationToken);
        return this.ReturnJson(accounts);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<BankAccountDetailsModel>>> GetAccount(long id, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.GetAccountAsync(id, cancellationToken);
        return this.ReturnJson(account);
    }

    [HttpPost] // Създаваме нова банкова сметка за клиент. Връща информация за новата сметка.
    public async Task<ActionResult<CommonJsonModel<BankAccountDetailsModel>>> CreateAccount(CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.CreateAccountAsync(request, cancellationToken);
        return this.ReturnJson(account);
    }

    [HttpPut("{id:long}/close")] // Затваряме банкова сметка за клиент. Връща информация за затворената сметка.
    public async Task<ActionResult<CommonJsonModel<BankAccountDetailsModel>>> CloseAccount(long id, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.CloseAccountAsync(id, cancellationToken);
        return this.ReturnJson(account);
    }
}
