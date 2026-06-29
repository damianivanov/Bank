using System.Security.Claims;
using System.Threading.RateLimiting;
using Bank.Core.Common;
using Bank.Core.Settings;

namespace Bank.Web.Infrastructure.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddBankRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var body = CommonJsonModel<string>.ErrorResult(
                    "You are sending requests too quickly. Please wait a moment and try again.");
                await context.HttpContext.Response.WriteAsJsonAsync(body, token);
            };

            // Анонимните извиквания са ограничени per (IP + подписан fingerprint); логнатите потребители са без лимит.
            o.AddPolicy("anon-calculator", httpContext =>
            {
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    return RateLimitPartition.GetNoLimiter("authenticated");
                }

                var settings = httpContext.RequestServices.GetRequiredService<ApplicationSettings>();
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var fingerprint = httpContext.Items.TryGetValue(AnonFingerprint.HttpContextItemsKey, out var value)
                    ? value?.ToString()
                    : null;
                var partitionKey = $"{ip}|{fingerprint ?? "no-fp"}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.CalculatorRateLimitPerHour,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
            });

            // Записващите операции с пари са лимитирани за логнат потребител. Заедно с
            // optimistic concurrency (RowVersion) и идемпотентните ключове това спира "спам" тегления/депозити.
            o.AddPolicy("money-operations", httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<ApplicationSettings>();
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter($"money:{userId}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.MoneyOperationsRateLimitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
            });
        });

        return services;
    }
}
