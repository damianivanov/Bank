using Bank.Core.Enums;
using Bank.DB.Entities;

namespace Bank.Services.MoneyOperations;

/// <summary>
/// Описва едно движение, което да бъде записано в регистъра чрез <see cref="IAccountLedger.Record"/>.
/// Контекстните идентификатори (<see cref="CreditId"/>, <see cref="CreditPaymentId"/>,
/// <see cref="DepositRequestId"/>) се попълват според вида на операцията и иначе остават null.
/// </summary>
public sealed record LedgerEntry
{
    /// <summary>Сметката, чието салдо се коригира.</summary>
    public required BankAccount Account { get; init; }

    /// <summary>Видът на движението (депозит, теглене, погасяване на кредит).</summary>
    public required MoneyTransactionType Type { get; init; }

    /// <summary>Брутната сума на движението (закръгля се до 2 знака от регистъра).</summary>
    public required decimal Amount { get; init; }

    /// <summary>Ключът за идемпотентност, който прави повторния запис безопасен.</summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>Кредитът, по който е движението — само при погасяване на кредит.</summary>
    public long? CreditId { get; init; }

    /// <summary>Конкретната вноска, която се погасява — само при погасяване на кредит.</summary>
    public long? CreditPaymentId { get; init; }

    /// <summary>Заявката за депозит, която се одобрява — само при депозит.</summary>
    public long? DepositRequestId { get; init; }

    /// <summary>Отпечатъкът на заявката, по който retry-ите се сверяват за идентичност.</summary>
    public string RequestHash { get; init; } = "";
}
