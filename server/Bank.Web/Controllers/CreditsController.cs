using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.DB.Constants;
using Bank.Services.Credits;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Roles = RoleNames.StaffOrAdmin)]
[Route("api/credits")]
public class CreditsController : BaseApiController
{
    private readonly ICreditService creditService;

    public CreditsController(ICreditService creditService)
    {
        this.creditService = creditService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<CreditModel>>>> GetCredits(CancellationToken cancellationToken)
    {
        var credits = await creditService.GetCreditsAsync(cancellationToken);
        return this.ReturnJson(credits);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<CreditDetailsModel>>> GetCredit(long id, CancellationToken cancellationToken)
    {
        var credit = await creditService.GetCreditAsync(id, cancellationToken);
        return this.ReturnJson(credit);
    }

    [HttpPost]
    public async Task<ActionResult<CommonJsonModel<CreditDetailsModel>>> CreateCredit(CreateCreditRequest request, CancellationToken cancellationToken)
    {
        var credit = await creditService.CreateCreditAsync(request, cancellationToken);
        return this.ReturnJson(credit);
    }

    [HttpPost("{creditId:long}/payments/{paymentId:long}/pay")]
    public async Task<ActionResult<CommonJsonModel<CreditDetailsModel>>> PayPayment(long creditId, long paymentId, CancellationToken cancellationToken)
    {
        var credit = await creditService.PayPaymentAsync(creditId, paymentId, cancellationToken);
        return this.ReturnJson(credit);
    }
}
