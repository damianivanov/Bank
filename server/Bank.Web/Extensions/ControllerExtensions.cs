using Bank.Core.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Extensions;

public static class ControllerExtensions
{
    public static JsonResult ReturnJson<T>(this ControllerBase controller, T data, string? warning = null)
    {
        var model = CommonJsonModel<T>.SuccessResult(data, warning);
        return new JsonResult(model);
    }

    public static ObjectResult ReturnError(this ControllerBase controller, string error, int statusCode = StatusCodes.Status400BadRequest)
    {
        var model = CommonJsonModel<string?>.ErrorResult(error);
        return new ObjectResult(model)
        {
            StatusCode = statusCode,
        };
    }

    public static ObjectResult ReturnError<T>(this ControllerBase controller, string error, T data, int statusCode = StatusCodes.Status400BadRequest)
    {
        var model = CommonJsonModel<T>.ErrorResult(error, data);
        return new ObjectResult(model)
        {
            StatusCode = statusCode,
        };
    }

    public static ObjectResult ReturnJsonError(this ControllerBase controller, string error)
    {
        return controller.ReturnError(error);
    }

    public static ObjectResult ReturnJsonError<T>(this ControllerBase controller, string error, T data)
    {
        return controller.ReturnError(error, data);
    }
}
