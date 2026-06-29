using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.CreditConditions;
using Bank.Services.CreditConditions;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Policy = Policies.RequireStaff)]
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

    [AllowAnonymous]
    [HttpGet("public")] // Взимаме публичните условия за кредити, които са видими за всички потребители (вкл. анонимни).
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<PublicCreditConditionModel>>>> GetPublicCreditConditions(CancellationToken cancellationToken)
    {
        var creditConditions = await creditConditionService.GetPublicCreditConditionsAsync(cancellationToken);
        return this.ReturnJson(creditConditions);
    }

    [Authorize(Policy = Policies.RequireAdmin)]
    [HttpPut("{id:long}")] // Актуализираме условията за кредит с даден ID. Връща информация за актуализираните условия.
    public async Task<ActionResult<CommonJsonModel<CreditTypeConditionModel>>> UpdateCreditCondition(long id, UpdateCreditConditionRequest request, CancellationToken cancellationToken)
    {
        var creditCondition = await creditConditionService.UpdateCreditConditionAsync(id, request, cancellationToken);
        return this.ReturnJson(creditCondition);
    }
}
