using Bank.Core.Common;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;
using Bank.Services.Credits;
using Bank.Services.Customers;
using Bank.Services.MoneyOperations;
using Bank.Services.Users;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bank.Web.Controllers;

/// <summary>
/// Самообслужване на клиента: четене на собствен профил и кредити, както и операции с пари върху
/// собствените сметки. Всеки endpoint резолвва person_id -> достъпни клиенти и подава ограничението
/// надолу към сървиса, така че клиент да не докосне чужда сметка/кредит. Записващите операции са
/// rate-limited per потребител, за да не може някой да "спами" 100 операции наведнъж.
/// </summary>
[Authorize(Policy = Policies.RequireCustomer)]
[Route("api/my-banking")]
public class MyBankingController : BaseApiController
{
    private readonly ICustomerService customerService;
    private readonly ICreditService creditService;
    private readonly IMoneyOperationService moneyOperationService;
    private readonly IUserService userService;

    public MyBankingController(
        ICustomerService customerService,
        ICreditService creditService,
        IMoneyOperationService moneyOperationService,
        IUserService userService)
    {
        this.customerService = customerService;
        this.creditService = creditService;
        this.moneyOperationService = moneyOperationService;
        this.userService = userService;
    }

    [HttpGet("customers")]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<CustomerLookupModel>>>> GetMyCustomers(CancellationToken cancellationToken)
    {
        var personId = userService.GetRequiredLoggedInPersonId();
        var customers = await customerService.GetAccessibleCustomersAsync(personId, cancellationToken);
        if (customers.Count == 0)
        {
            throw new BankException("Към този акаунт няма свързан клиент.", 404);
        }

        return this.ReturnJson(customers);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> GetProfile([FromQuery] long? customerId, CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);

        var targetCustomerId = customerId ?? accessibleCustomerIds.First();
        if (!accessibleCustomerIds.Contains(targetCustomerId))
        {
            throw new BankException("Клиентът не е намерен.", 404);
        }

        var customer = await customerService.GetCustomerAsync(targetCustomerId, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpGet("credits/{creditId:long}")]
    public async Task<ActionResult<CommonJsonModel<CreditDetailsModel>>> GetCredit(long creditId, CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);
        var credit = await creditService.GetCreditForCustomersAsync(accessibleCustomerIds, creditId, cancellationToken);
        return this.ReturnJson(credit);
    }

    [HttpPost("accounts/{accountId:long}/deposit-requests")]
    [EnableRateLimiting("money-operations")]
    public async Task<ActionResult<CommonJsonModel<DepositRequestModel>>> RequestDeposit(
        long accountId,
        DepositRequestCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);
        var result = await moneyOperationService.RequestDepositAsync(accessibleCustomerIds, accountId, request, cancellationToken);
        return this.ReturnJson(result);
    }

    [HttpPost("accounts/{accountId:long}/withdrawals")]
    [EnableRateLimiting("money-operations")]
    public async Task<ActionResult<CommonJsonModel<AccountOperationResultModel>>> Withdraw(
        long accountId,
        WithdrawalCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);
        var result = await moneyOperationService.WithdrawAsync(accessibleCustomerIds, accountId, request, cancellationToken);
        return this.ReturnJson(result);
    }

    [HttpPost("credits/{creditId:long}/pay-installment")]
    [EnableRateLimiting("money-operations")]
    public async Task<ActionResult<CommonJsonModel<CreditInstallmentPaymentResultModel>>> PayInstallment(
        long creditId,
        PayCreditInstallmentRequest request,
        CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);
        var result = await moneyOperationService.PayCreditInstallmentAsync(accessibleCustomerIds, creditId, request, cancellationToken);
        return this.ReturnJson(result);
    }

    [HttpGet("accounts/{accountId:long}/transactions")]
    public async Task<ActionResult<CommonJsonModel<PagedResponse<MoneyTransactionModel>>>> GetTransactions(
        long accountId,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);
        var transactions = await moneyOperationService.GetAccountTransactionsAsync(accessibleCustomerIds, accountId, request, cancellationToken);
        return this.ReturnJson(transactions);
    }

    [HttpGet("deposit-requests")]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<DepositRequestModel>>>> GetMyDepositRequests(
        CancellationToken cancellationToken)
    {
        var accessibleCustomerIds = await ResolveAccessibleCustomerIdsAsync(cancellationToken);
        var requests = await moneyOperationService.GetMyDepositRequestsAsync(accessibleCustomerIds, cancellationToken);
        return this.ReturnJson(requests);
    }

    private async Task<IReadOnlyCollection<long>> ResolveAccessibleCustomerIdsAsync(CancellationToken cancellationToken)
    {
        var personId = userService.GetRequiredLoggedInPersonId();
        var accessibleCustomerIds = await customerService.GetAccessibleCustomerIdsAsync(personId, cancellationToken);
        if (accessibleCustomerIds.Count == 0)
        {
            throw new BankException("Към този акаунт няма свързан клиент.", 404);
        }

        return accessibleCustomerIds;
    }
}
