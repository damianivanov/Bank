using Bank.DB.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Bank.Web.Infrastructure;

public class AdminAuthorizationHandler : AuthorizationHandler<AdminAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminAuthorizationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
