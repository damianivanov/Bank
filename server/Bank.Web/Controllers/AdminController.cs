using Bank.Core.Common;
using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Common;
using Bank.Core.JsonModels.Diagnostics;
using Bank.Services.Diagnostics;
using Bank.Services.Users.Administration;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Policy = Policies.RequireAdmin)]
[Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly IUserAdministrationService userAdministrationService;
    private readonly IErrorService errorService;

    public AdminController(IUserAdministrationService userAdministrationService, IErrorService errorService)
    {
        this.userAdministrationService = userAdministrationService;
        this.errorService = errorService;
    }

    [HttpGet("users")] // Взимаме списък с всички потребители + служители, с възможност за филтриране по роли и статус.
    public async Task<ActionResult<CommonJsonModel<UserAccessPageModel>>> GetUsersForAdministration(
        [FromQuery] PagedRequest request,
        [FromQuery] UserRole[]? roles,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var users = await userAdministrationService.GetUsersForAdministrationAsync(request, roles ?? Array.Empty<UserRole>(), isActive, cancellationToken);
        return this.ReturnJson(users);
    }

    [HttpPut("users/{id:long}/access")] // Актуализираме достъпа на потребител.
    public async Task<ActionResult<CommonJsonModel<UserAccessModel>>> UpdateUserAccess(long id, UpdateUserAccessRequest request, CancellationToken cancellationToken)
    {
        var user = await userAdministrationService.UpdateUserAccessAsync(id, request, cancellationToken);
        return this.ReturnJson(user);
    }

    [HttpGet("errors")] // Взимаме списък с всички грешки, с възможност за филтриране по дата и търсене по съобщение.
    public async Task<ActionResult<CommonJsonModel<PagedResponse<ApiErrorModel>>>> GetErrors(
        [FromQuery] PagedRequest request,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var errors = await errorService.GetErrorsAsync(request, fromDate, toDate, cancellationToken);
        return this.ReturnJson(errors);
    }
}
