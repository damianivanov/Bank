using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Web.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected long? CurrentCustomerId
    {
        get
        {
            var value = User.FindFirstValue("customer_id");
            return long.TryParse(value, out var customerId) ? customerId : null;
        }
    }
}
