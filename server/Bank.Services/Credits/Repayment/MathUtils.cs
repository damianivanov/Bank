namespace Bank.Services.Credits;

public static class MathUtils
{
    public static decimal Pow(decimal value, int exponent)
    {
        if (exponent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), "Exponent cannot be negative.");
        }

        var result = 1m;
        for (var index = 0; index < exponent; index++)
        {
            result *= value;
        }

        return result;
    }
}
