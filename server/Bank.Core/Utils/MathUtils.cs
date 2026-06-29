namespace Bank.Core.Utils;

public static class MathUtils
{
    //нужна ми е собствена реализация на Pow за decimal, защото Math.Pow работи с double и може да доведе до загуба на точност при финансови калкулации.
    public static decimal Pow(decimal baseValue, int exponent)
    {
        if (exponent == 0) return 1m;
        if (exponent < 0) return 1m / Pow(baseValue, -exponent);
        if (baseValue == 0m) return 0m;

        decimal result = 1m;
        decimal @base = baseValue;
        int n = exponent;

        while (n > 0)
        {
            if ((n & 1) != 0)
                result *= @base;
            @base *= @base;
            n >>= 1;
        }
        return result;
    }

    public static decimal PercentOf(decimal value, decimal percent) => value * (percent / 100m);

}
