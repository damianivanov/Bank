using System.Text;

namespace Bank.Services.Accounts;

public static class IbanValidator
{
    public static string Normalize(string iban)
    {
        var buffer = new StringBuilder(iban.Length);
        foreach (var character in iban)
        {
            if (!char.IsWhiteSpace(character))
            {
                buffer.Append(char.ToUpperInvariant(character));
            }
        }

        return buffer.ToString();
    }

    public static bool IsValid(string normalizedIban)
    {
        if (string.IsNullOrWhiteSpace(normalizedIban) || normalizedIban.Length is < 15 or > 34)
        {
            return false;
        }

        if (!char.IsLetter(normalizedIban[0]) || !char.IsLetter(normalizedIban[1]))
        {
            return false;
        }

        if (!char.IsDigit(normalizedIban[2]) || !char.IsDigit(normalizedIban[3]))
        {
            return false;
        }

        if (normalizedIban.Any(character => !char.IsLetterOrDigit(character)))
        {
            return false;
        }

        return HasValidMod97(normalizedIban);
    }

    private static bool HasValidMod97(string normalizedIban)
    {
        var rearranged = normalizedIban[4..] + normalizedIban[..4];
        var remainder = 0;

        foreach (var character in rearranged)
        {
            if (char.IsDigit(character))
            {
                remainder = (remainder * 10 + (character - '0')) % 97;
                continue;
            }

            var mappedValue = character - 'A' + 10;
            var valueText = mappedValue.ToString();
            foreach (var digit in valueText)
            {
                remainder = (remainder * 10 + (digit - '0')) % 97;
            }
        }

        return remainder == 1;
    }
}
