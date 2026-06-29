using Bank.Core.Common;
using Bank.Core.Enums;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;
using Bank.Services.MoneyOperations;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

/// <summary>
/// Staff-facing опашка за одобрение на заявки за депозит. Одобрението е единственото нещо, което кредитира
/// салдо при депозит — затова е зад RequireStaff и не може да бъде извикано от клиент.
/// </summary>
[Authorize(Policy = Policies.RequireStaff)]
[Route("api/deposit-requests")]
public class DepositApprovalsController : BaseApiController
{
    private readonly IDepositApprovalService depositApprovalService;

    public DepositApprovalsController(IDepositApprovalService depositApprovalService)
    {
        this.depositApprovalService = depositApprovalService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<PagedResponse<DepositRequestQueueModel>>>> GetDepositRequests(
        [FromQuery] DepositRequestStatus? status,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var requests = await depositApprovalService.GetDepositRequestsAsync(status, request, cancellationToken);
        return this.ReturnJson(requests);
    }

    [HttpPost("{id:long}/approve")]
    public async Task<ActionResult<CommonJsonModel<AccountOperationResultModel>>> Approve(long id, CancellationToken cancellationToken)
    {
        var result = await depositApprovalService.ApproveAsync(id, cancellationToken);
        return this.ReturnJson(result);
    }

    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult<CommonJsonModel<DepositRequestQueueModel>>> Reject(
        long id,
        DepositRejectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await depositApprovalService.RejectAsync(id, request, cancellationToken);
        return this.ReturnJson(result);
    }
}
