using Bank.Core.Common;
using Bank.Core.Enums;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.JsonModels.Common;
using Bank.Services.Customers;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Bank.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Authorize(Policy = Policies.RequireStaff)]
[Route("api/customers")]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService customerService;

    public CustomersController(ICustomerService customerService)
    {
        this.customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<CommonJsonModel<PagedResponse<CustomerModel>>>> GetCustomers([FromQuery] PagedRequest request, [FromQuery] CustomerType? customerType, CancellationToken cancellationToken)
    {
        var customers = await customerService.GetCustomersAsync(request, customerType, cancellationToken);
        return this.ReturnJson(customers);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<CommonJsonModel<IReadOnlyCollection<CustomerLookupModel>>>> GetCustomerLookup([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var customers = await customerService.GetCustomerLookupAsync(search, cancellationToken);
        return this.ReturnJson(customers);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> GetCustomer(long id, CancellationToken cancellationToken)
    {
        var customer = await customerService.GetCustomerAsync(id, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpGet("{id:long}/edit")]
    public async Task<ActionResult<CommonJsonModel<CustomerEditModel>>> GetCustomerForEdit(long id, CancellationToken cancellationToken)
    {
        var customer = await customerService.GetCustomerForEditAsync(id, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CommonJsonModel<CustomerModel>>> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.CreateCustomerAsync(request, cancellationToken);
        return this.ReturnJson(customer);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CommonJsonModel<CustomerModel>>> UpdateCustomer(long id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.UpdateCustomerAsync(id, request, cancellationToken);
        return this.ReturnJson(customer);
    }

    // VIP е решение на гишето: служителите, които работят с клиенти, дават преференциалните
    // условия, затова това наследява RequireStaff политиката на ниво контролер (админите също отговарят).
    [HttpPut("{id:long}/vip")]
    public async Task<ActionResult<CommonJsonModel<CustomerDetailsModel>>> UpdateCustomerVip(long id, UpdateCustomerVipRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.UpdateVipAsync(id, request, cancellationToken);
        return this.ReturnJson(customer);
    }
}
