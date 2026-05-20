using Bank.Core.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Web.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<CommonJsonModel<T>> OkData<T>(T data)
    {
        return Ok(CommonJsonModel<T>.SuccessResult(data));
    }

    protected long? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return long.TryParse(value, out var userId) ? userId : null;
        }
    }
}
