using Bank.Core.Common;
using Bank.Core.Exceptions;
using Bank.DB;
using Bank.DB.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bank.Web.Attributes;

public class LogApiErrorAttribute : IAsyncExceptionFilter
{
    private readonly AppDbContext dbContext;
    private readonly ILogger<LogApiErrorAttribute> logger;

    public LogApiErrorAttribute(AppDbContext dbContext, ILogger<LogApiErrorAttribute> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        logger.LogError(context.Exception, "API request failed at {Path}", context.HttpContext.Request.Path);

        dbContext.Errors.Add(new Error
        {
            Message = context.Exception.Message,
            Details = context.Exception.ToString(),
            Path = context.HttpContext.Request.Path,
            UserName = context.HttpContext.User.Identity?.Name,
        });

        await dbContext.SaveChangesAsync();

        var statusCode = context.Exception is BankException bankException
            ? bankException.StatusCode
            : StatusCodes.Status500InternalServerError;

        var message = context.Exception is BankException
            ? context.Exception.Message
            : "Unexpected server error.";

        context.Result = new ObjectResult(CommonJsonModel<string>.ErrorResult(message))
        {
            StatusCode = statusCode,
        };
        context.ExceptionHandled = true;
    }
}
