using Bank.Core.Settings;

namespace Bank.Web.Infrastructure.RateLimiting;

// Гарантира, че всяка calculator заявка носи валидиран анонимен fingerprint, като оставя разрешения id
// в HttpContext.Items, за да може лимитерът да партиционира по него още при първото попадение (преди
// Set-Cookie да се е върнал от браузъра).
public class AnonFingerprintMiddleware
{
    private const string CalculatorPathPrefix = "/api/calculators";

    private readonly RequestDelegate next;

    public AnonFingerprintMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationSettings settings, IHostEnvironment environment)
    {
        if (context.Request.Path.StartsWithSegments(CalculatorPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var signingKey = settings.AnonFingerprintSigningKey;
            var cookieValue = context.Request.Cookies[AnonFingerprint.CookieName];

            if (!AnonFingerprint.TryValidate(cookieValue, signingKey, out var id))
            {
                id = AnonFingerprint.Issue(signingKey, out var newCookieValue);
                context.Response.Cookies.Append(AnonFingerprint.CookieName, newCookieValue, BuildCookieOptions(environment));
            }

            context.Items[AnonFingerprint.HttpContextItemsKey] = id;
        }

        await next(context);
    }

    private static CookieOptions BuildCookieOptions(IHostEnvironment environment)
    {
        var isDevelopment = environment.IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Secure = !isDevelopment,
            Expires = DateTimeOffset.UtcNow.AddDays(400),
        };
    }
}
