using Bank.Core.Common;
using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.DB.Constants;
using Bank.Services.Customers;
using Bank.Services.Users;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Roles = RoleNames.StaffOrAdmin)]
[Route("api/users")]
public class UsersController : BaseApiController
{
    private readonly IUserService userService;
    private readonly ICustomerService customerService;

    public UsersController(IUserService userService, ICustomerService customerService)
    {
        this.userService = userService;
        this.customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<StaffUserGridModel>>>> GetCustomerUsers(CancellationToken cancellationToken)
    {
        var users = await userService.GetStaffUsersForManagementAsync(cancellationToken);
        return this.ReturnJson(users);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<UserAccessModel>>> GetUser(long id, CancellationToken cancellationToken)
    {
        var user = await userService.GetUserForAdministrationAsync(id, cancellationToken);
        return this.ReturnJson(user);
    }

    [HttpPost("{id:long}/customer")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> CreateCustomerForUser(long id, CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.CreateCustomerForUserAsync(id, request, cancellationToken);
        return this.ReturnJson(customer);
    }
}
