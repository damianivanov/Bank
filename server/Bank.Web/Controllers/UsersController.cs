using Bank.Core.Common;
using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.JsonModels.Common;
using Bank.Services.Customers;
using Bank.Services.Users.Administration;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Policy = Policies.RequireStaff)]
[Route("api/users")]
public class UsersController : BaseApiController
{
    private readonly IUserAdministrationService userAdministrationService;
    private readonly ICustomerService customerService;

    public UsersController(IUserAdministrationService userAdministrationService, ICustomerService customerService)
    {
        this.userAdministrationService = userAdministrationService;
        this.customerService = customerService;
    }

    [HttpGet] // Взимаме списък с всички потребители, които не са администратори.
    public async Task<ActionResult<CommonJsonModel<StaffUserPageModel>>> GetRegularUsers(
        [FromQuery] PagedRequest request,
        [FromQuery] bool? linked,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var users = await userAdministrationService.GetRegularUsersAsync(request, linked, isActive, cancellationToken);
        return this.ReturnJson(users);
    }

    [HttpGet("{id:long}")] // Взимаме информация за 1 потребител по ID.
    public async Task<ActionResult<CommonJsonModel<UserAccessModel>>> GetUser(long id, CancellationToken cancellationToken)
    {
        var user = await userAdministrationService.GetUserForAdministrationAsync(id, cancellationToken);
        return this.ReturnJson(user);
    }

    [HttpPost("{id:long}/customer")] // Създаваме нов клиент за даден потребител (ако няма такъв). Връща информация за новия клиент.
    public async Task<ActionResult<CommonJsonModel<CustomerModel>>> CreateCustomerForUser(long id, CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.CreateCustomerForUserAsync(id, request, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpPost] // Регистрираме нов клиент.
    public async Task<ActionResult<CommonJsonModel<CustomerModel>>> RegisterCounterCustomer(RegisterCounterCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.RegisterCounterCustomerAsync(request, cancellationToken);
        return this.ReturnJson(customer);
    }
}
