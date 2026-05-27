using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.Exceptions;
using Bank.DB.Constants;
using Bank.Services.Customers;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/customer-self")]
public class CustomerSelfController : BaseApiController
{
    private readonly ICustomerService customerService;

    public CustomerSelfController(ICustomerService customerService)
    {
        this.customerService = customerService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> GetProfile(CancellationToken cancellationToken)
    {
        var customerId = CurrentCustomerId ?? throw new BankException("Customer context is missing.", 401);
        var customer = await customerService.GetCustomerAsync(customerId, cancellationToken);
        return this.ReturnJson(customer);
    }
}
