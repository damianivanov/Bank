using Bank.Core.Common;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bank.Web.Controllers;

[Route("api/calculators")]
public class CalculatorsController : BaseApiController
{
    private readonly ICreditCalculatorService creditCalculatorService;
    private readonly ILeasingCalculatorService leasingCalculatorService;
    private readonly IRefinancingCalculatorService refinancingCalculatorService;

    public CalculatorsController(
        ICreditCalculatorService creditCalculatorService,
        ILeasingCalculatorService leasingCalculatorService,
        IRefinancingCalculatorService refinancingCalculatorService)
    {
        this.creditCalculatorService = creditCalculatorService;
        this.leasingCalculatorService = leasingCalculatorService;
        this.refinancingCalculatorService = refinancingCalculatorService;
    }

    // Публично, но анонимните извиквания са ограничени с лимит на час..
    [AllowAnonymous]
    [HttpPost("credit")]
    [EnableRateLimiting("anon-calculator")]
    public async Task<ActionResult<CommonJsonModel<CreditCalculatorResponse>>> CalculateCredit(CreditCalculatorRequest request)
    {
        var result = await creditCalculatorService.CalculateAsync(request);
        return this.ReturnJson(result);
    }

    [Authorize]
    [HttpPost("leasing")] // само за логнати потребители.
    public async Task<ActionResult<CommonJsonModel<LeasingCalculatorResponse>>> CalculateLeasing(LeasingCalculatorRequest request)
    {
        var result = await leasingCalculatorService.CalculateAsync(request);
        return this.ReturnJson(result);
    }

    [Authorize]
    [HttpPost("refinancing")] // само за логнати потребители.
    public async Task<ActionResult<CommonJsonModel<RefinancingCalculatorResponse>>> CalculateRefinancing(RefinancingCalculatorRequest request)
    {
        var result = await refinancingCalculatorService.CalculateAsync(request);
        return this.ReturnJson(result);
    }
}
