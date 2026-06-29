using Microsoft.Extensions.Configuration;

namespace Bank.Core.Settings;

public class ApplicationSettings
{
    private readonly IConfiguration configuration;

    public ApplicationSettings(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public string JwtSigningKey => GetSetting("Jwt:SigningKey")
        ?? throw new InvalidOperationException("JWT signing key not configured.");

    public string JwtIssuer => GetSetting("Jwt:Issuer") ?? "Bank";
    public string JwtAudience => GetSetting("Jwt:Audience") ?? "Bank";
    public int JwtExpirationMinutes => ParseOrDefault(GetSetting("Jwt:ExpirationMinutes"), 15);

    public string RefreshTokenSigningKey => GetSetting("RefreshToken:SigningKey") ?? JwtSigningKey;
    public string RefreshTokenIssuer => GetSetting("RefreshToken:Issuer") ?? JwtIssuer;
    public string RefreshTokenAudience => GetSetting("RefreshToken:Audience") ?? JwtAudience;
    public int RefreshTokenExpirationDays => ParseOrDefault(GetSetting("RefreshToken:ExpirationDays"), 14);

    public string ClientUrl => GetSetting("Application:ClientUrl") ?? "http://localhost:3001";

    public int CalculatorRateLimitPerHour => ParseOrDefault(GetSetting("Application:CalculatorRateLimitPerHour"), 15);

    // Таван на записващите операции с пари (депозит/теглене/вноска) на минута per логнат потребител —
    // спира "спам" от 100 заявки наведнъж. Конкурентната коректност се пази отделно от RowVersion-а.
    public int MoneyOperationsRateLimitPerMinute => ParseOrDefault(GetSetting("Application:MoneyOperationsRateLimitPerMinute"), 10);

    public string AnonFingerprintSigningKey => GetSetting("Application:AnonFingerprintSigningKey") ?? RefreshTokenSigningKey;

    private string? GetSetting(string key)
    {
        return configuration[key];
    }

    private static int ParseOrDefault(string? value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
