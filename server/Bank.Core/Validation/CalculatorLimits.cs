namespace Bank.Core.Validation;

// Споделени числови граници за кредитните калкулатори, ползвани едновременно от [Range] атрибутите на request-моделите
// и от calculator услугите, за да не могат двата слоя на валидация да се разминат.
public static class CalculatorLimits
{
    public const int MinTermMonths = 1; // Минимален срок на кредита/лизинга в месеци
    public const int MaxTermMonths = 360; // Максимален срок в месеци (30 години)

    public const double MinAmount = 0.01; // Минимална положителна сума (напр. размер на кредита)
    public const double MaxAmount = 1_000_000_000d; // Максимална сума, таван срещу нереалистични стойности

    public const double MinNonNegativeAmount = 0d; // Минимум за суми, които може да са 0 (напр. първоначална вноска, такси)

    public const double MinRate = 0d; // Минимален лихвен процент
    public const double MaxRate = 1000d; // Максимален лихвен процент, таван срещу абсурдни стойности

    public const double MinPercent = 0d; // Минимум за процентни полета (напр. такси в %)
    public const double MaxPercent = 100d; // Максимум за процентни полета — не може над 100%
}
