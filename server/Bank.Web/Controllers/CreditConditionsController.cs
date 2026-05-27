using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.CreditConditions;
using Bank.DB.Constants;
using Bank.Services.CreditConditions;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Roles = RoleNames.StaffOrAdmin)]
[Route("api/credit-conditions")]
public class CreditConditionsController : BaseApiController
{
    private readonly ICreditConditionService creditConditionService;

    public CreditConditionsController(ICreditConditionService creditConditionService)
    {
        this.creditConditionService = creditConditionService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<CreditTypeConditionModel>>>> GetCreditConditions(CancellationToken cancellationToken)
    {
        var creditConditions = await creditConditionService.GetCreditConditionsAsync(cancellationToken);
        return this.ReturnJson(creditConditions);
    }
}
