namespace Bank.Services.Common;

public static class BulgarianIdentifierValidator
{
    private static readonly int[] EgnWeights = [2, 4, 8, 5, 10, 9, 7, 3, 6];
    private static readonly int[] EikPrimaryWeights = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly int[] EikSecondaryWeights = [3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly int[] EikBranchPrimaryWeights = [2, 7, 3, 5];
    private static readonly int[] EikBranchSecondaryWeights = [4, 9, 5, 7];

    public static bool IsValidEgn(string? value)
    {
        if (!TryParseDigits(value, expectedLength: 10, out var digits))
        {
            return false;
        }

        if (!HasValidEgnBirthDate(digits))
        {
            return false;
        }

        var checksum = CalculateModuloElevenChecksum(digits, EgnWeights, startIndex: 0);
        return checksum == digits[9];
    }

    public static bool IsValidEik(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || (value.Length != 9 && value.Length != 13))
        {
            return false;
        }

        if (!TryParseDigits(value, expectedLength: value.Length, out var digits))
        {
            return false;
        }

        if (CalculateFallbackChecksum(digits, EikPrimaryWeights, EikSecondaryWeights, startIndex: 0) != digits[8])
        {
            return false;
        }

        return digits.Length == 9
            || CalculateFallbackChecksum(digits, EikBranchPrimaryWeights, EikBranchSecondaryWeights, startIndex: 8) == digits[12];
    }

    private static bool HasValidEgnBirthDate(IReadOnlyList<int> digits)
    {
        var year = digits[0] * 10 + digits[1];
        var month = digits[2] * 10 + digits[3];
        var day = digits[4] * 10 + digits[5];

        if (month is >= 1 and <= 12)
        {
            year += 1900;
        }
        else if (month is >= 21 and <= 32)
        {
            year += 1800;
            month -= 20;
        }
        else if (month is >= 41 and <= 52)
        {
            year += 2000;
            month -= 40;
        }
        else
        {
            return false;
        }

        return DateOnly.TryParseExact(
            $"{year:D4}-{month:D2}-{day:D2}",
            "yyyy-MM-dd",
            out _);
    }

    private static int CalculateModuloElevenChecksum(IReadOnlyList<int> digits, IReadOnlyList<int> weights, int startIndex)
    {
        var sum = 0;
        for (var index = 0; index < weights.Count; index++)
        {
            sum += digits[startIndex + index] * weights[index];
        }

        var checksum = sum % 11;
        return checksum == 10 ? 0 : checksum;
    }

    private static int CalculateFallbackChecksum(
        IReadOnlyList<int> digits,
        IReadOnlyList<int> primaryWeights,
        IReadOnlyList<int> secondaryWeights,
        int startIndex)
    {
        var checksum = CalculateRawModuloElevenChecksum(digits, primaryWeights, startIndex);
        if (checksum != 10)
        {
            return checksum;
        }

        checksum = CalculateRawModuloElevenChecksum(digits, secondaryWeights, startIndex);
        return checksum == 10 ? 0 : checksum;
    }

    private static int CalculateRawModuloElevenChecksum(IReadOnlyList<int> digits, IReadOnlyList<int> weights, int startIndex)
    {
        var sum = 0;
        for (var index = 0; index < weights.Count; index++)
        {
            sum += digits[startIndex + index] * weights[index];
        }

        return sum % 11;
    }

    private static bool TryParseDigits(string? value, int expectedLength, out int[] digits)
    {
        digits = [];
        if (string.IsNullOrWhiteSpace(value) || value.Length != expectedLength)
        {
            return false;
        }

        digits = new int[value.Length];
        for (var index = 0; index < value.Length; index++)
        {
            if (!char.IsDigit(value[index]))
            {
                digits = [];
                return false;
            }

            digits[index] = value[index] - '0';
        }

        return true;
    }
}
