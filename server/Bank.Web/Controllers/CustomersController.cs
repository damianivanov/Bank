using Bank.Core.Common;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.DB.Constants;
using Bank.Services.Customers;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Roles = RoleNames.StaffOrAdmin)]
[Route("api/customers")]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService customerService;

    public CustomersController(ICustomerService customerService)
    {
        this.customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<CustomerModel>>>> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await customerService.GetCustomersAsync(cancellationToken);
        return this.ReturnJson(customers);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<CustomerLookupModel>>>> GetCustomerLookup(CancellationToken cancellationToken)
    {
        var customers = await customerService.GetCustomerLookupAsync(cancellationToken);
        return this.ReturnJson(customers);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> GetCustomer(long id, CancellationToken cancellationToken)
    {
        var customer = await customerService.GetCustomerAsync(id, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.CreateCustomerAsync(request, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> UpdateCustomer(long id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.UpdateCustomerAsync(id, request, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpPut("{id:long}/vip")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> UpdateCustomerVip(long id, UpdateCustomerVipRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.UpdateVipAsync(id, request, cancellationToken);
        return this.ReturnJson(customer);
    }
}
