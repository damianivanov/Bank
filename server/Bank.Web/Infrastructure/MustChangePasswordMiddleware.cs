using Bank.Core.Common;
using Bank.DB.Constants;
using System.Security.Claims;
using System.Text.Json;

namespace Bank.Web.Infrastructure;

// Fail-closed порта: потребител с активна принудителна смяна на парола (създаден на гише,
// парола = ЕГН) не достъпва нищо освен смяната на паролата, излизане и справка за себе си —
// докато не я смени. Решава се по claim-а в токена; новата сесия след смяна вече няма claim-а.
public sealed class MustChangePasswordMiddleware
{
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/change-password",
        "/api/auth/logout",
        "/api/auth/current-user",
        "/api/auth/refresh",
    ];

    private readonly RequestDelegate next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresPasswordChange(context.User) && !IsAllowed(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var payload = CommonJsonModel<string>.ErrorResult("Трябва да смените паролата си, преди да продължите.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            return;
        }

        await next(context);
    }

    private static bool RequiresPasswordChange(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
            && string.Equals(user.FindFirstValue(ClaimNames.MustChangePassword), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowed(PathString path)
    {
        return AllowedPaths.Any(allowed => path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
