using Bank.Core.Common;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using Bank.Services.Users;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize]
[Route("api/saved-calculations")]
public class SavedCalculationsController : BaseApiController
{
    private readonly ISavedCalculationService savedCalculationService;
    private readonly IUserService userService;

    public SavedCalculationsController(ISavedCalculationService savedCalculationService, IUserService userService)
    {
        this.savedCalculationService = savedCalculationService;
        this.userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<SavedCalculationModel>>>> GetSavedCalculations(CancellationToken cancellationToken)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var items = await savedCalculationService.ListAsync(userId, cancellationToken);
        return this.ReturnJson(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<SavedCalculationDetailsModel>>> GetSavedCalculation(long id, CancellationToken cancellationToken)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var item = await savedCalculationService.GetAsync(userId, id, cancellationToken);
        return this.ReturnJson(item);
    }

    [HttpPost]
    public async Task<ActionResult<CommonJsonModel<SavedCalculationModel>>> SaveCalculation(SaveCalculationRequest request, CancellationToken cancellationToken)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var saved = await savedCalculationService.SaveAsync(userId, request, cancellationToken);
        return this.ReturnJson(saved);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<SavedCalculationModel>>> UpdateSavedCalculation(long id, SaveCalculationRequest request, CancellationToken cancellationToken)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var updated = await savedCalculationService.UpdateAsync(userId, id, request, cancellationToken);
        return this.ReturnJson(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<string>>> DeleteSavedCalculation(long id, CancellationToken cancellationToken)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        await savedCalculationService.DeleteAsync(userId, id, cancellationToken);
        return this.ReturnJson("Deleted");
    }
}
