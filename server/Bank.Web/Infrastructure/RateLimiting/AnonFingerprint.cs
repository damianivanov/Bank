using System.Security.Cryptography;
using System.Text;

namespace Bank.Web.Infrastructure.RateLimiting;

// Подписан fingerprint на анонимен посетител: cookie-то носи случаен id + HMAC подпис, така че не може да бъде
// фалшифицирано или сменяно за нов. Изтриването му все пак е възможно, затова ключът на rate-limit-а
// включва и IP-то на клиента (не може да промени).
public static class AnonFingerprint
{
    public const string CookieName = "calc_fp";
    public const string HttpContextItemsKey = "calc_fp_value";

    public static string Issue(string signingKey, out string cookieValue)
    {
        var id = Guid.NewGuid().ToString("N");
        cookieValue = $"{id}.{ComputeSignature(id, signingKey)}";
        return id;
    }

    public static bool TryValidate(string? cookieValue, string signingKey, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(cookieValue))
        {
            return false;
        }

        var separatorIndex = cookieValue.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex >= cookieValue.Length - 1)
        {
            return false;
        }

        var candidateId = cookieValue[..separatorIndex];
        var signature = cookieValue[(separatorIndex + 1)..];
        var expected = ComputeSignature(candidateId, signingKey);

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expected)))
        {
            return false;
        }

        id = candidateId;
        return true;
    }

    private static string ComputeSignature(string id, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(id));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
