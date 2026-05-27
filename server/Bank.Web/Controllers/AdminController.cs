using Bank.Core.Common;
using Bank.Core.JsonModels.Auth;
using Bank.Services.Users;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly IUserService userService;

    public AdminController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<UserAccessModel>>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await userService.GetUsersForAdministrationAsync(cancellationToken);
        return this.ReturnJson(users);
    }

    [HttpPut("users/{id:long}/access")]
    public async Task<ActionResult<CommonJsonModel<UserAccessModel>>> UpdateUserAccess(long id, UpdateUserAccessRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.UpdateUserAccessAsync(id, request, cancellationToken);
        return this.ReturnJson(user);
    }
}
