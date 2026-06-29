using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Services.MoneyOperations;

/// <summary>
/// Прави канонски, културно-независим SHA-256 отпечатък на бизнес-входа на дадена операция с пари.
/// Служи за разграничаване на честен retry от подмяна на тялото при същия idempotency ключ:
/// същ ключ + същ отпечатък -> връщаме стария резултат; същ ключ + друг отпечатък -> 409 конфликт.
/// </summary>
internal static class IdempotencyHash
{
    public static string Compute(string operation, params (string Field, string Value)[] fields)
    {
        var canonical = new StringBuilder(operation);
        foreach (var (field, value) in fields)
        {
            canonical.Append('|').Append(field).Append('=').Append(value);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Нормализира сума до стабилен, културно-независим текст, за да съвпада отпечатъкът при retry.</summary>
    public static string Amount(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>Цяло число към културно-независим текст (Id-та на сметки/кредити).</summary>
    public static string Id(long value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Незадължителен Id към текст; <c>null</c> става стабилен маркер, за да не съвпада с реален Id.</summary>
    public static string OptionalId(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "auto";
}
