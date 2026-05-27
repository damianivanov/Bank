using System.Security.Cryptography;

namespace Bank.Services.Accounts;

public class IbanGenerator : IIbanGenerator
{
    private const string CountryCode = "BG";
    private const string BankCode = "BANK";
    private const string BranchCode = "0001";
    private const string AccountTypeCode = "00";

    public string Generate()
    {
        var accountNumber = RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");
        var bban = $"{BankCode}{BranchCode}{AccountTypeCode}{accountNumber}";
        var checkDigits = CalculateCheckDigits(CountryCode, bban);

        return $"{CountryCode}{checkDigits}{bban}";
    }

    private static string CalculateCheckDigits(string countryCode, string bban)
    {
        var rearranged = bban + countryCode + "00";
        var remainder = 0;

        foreach (var character in rearranged)
        {
            if (char.IsDigit(character))
            {
                remainder = (remainder * 10 + (character - '0')) % 97;
                continue;
            }

            if (char.IsLetter(character))
            {
                var mappedValue = char.ToUpperInvariant(character) - 'A' + 10;
                var valueText = mappedValue.ToString();

                foreach (var digit in valueText)
                {
                    remainder = (remainder * 10 + (digit - '0')) % 97;
                }
            }
        }

        var checkDigits = 98 - remainder;
        return checkDigits.ToString("D2");
    }
}
