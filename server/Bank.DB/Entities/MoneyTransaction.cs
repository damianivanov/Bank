using System.ComponentModel.DataAnnotations;
using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

/// <summary>
/// Неизменим ред в счетоводния регистър (ledger): по едно вписване за всяко движение на пари по сметка —
/// одобрен депозит, теглене или вноска по кредит. <see cref="Amount"/> е винаги положителна величина;
/// посоката се извежда от <see cref="Type"/>. <see cref="BalanceAfter"/> е снимка на салдото веднага след
/// движението, за да може всяко движение да се одитира независимо от денормализираното салдо по сметката.
/// </summary>
public class MoneyTransaction : BaseTrackUserEntity
{
    public long BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public MoneyTransactionType Type { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }

    // Попълнени само за движения от тип CreditPayment.
    public long? CreditId { get; set; }
    public Credit? Credit { get; set; }

    public long? CreditPaymentId { get; set; }
    public CreditInstallment? CreditInstallment { get; set; }

    // Попълнено само за движения от тип Deposit (произходът — одобрената заявка).
    public long? DepositRequestId { get; set; }
    public DepositRequest? DepositRequest { get; set; }

    // Уникален ключ за идемпотентност: повторно изпратено движение не се записва втори път.
    [MaxLength(80)]
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    // Отпечатък (SHA-256) на бизнес-входа на операцията. Същ ключ + същ отпечатък = честен retry;
    // същ ключ + друг отпечатък = опит за подмяна на тялото -> 409. Празно за движения без клиентско тяло
    // (напр. одобрен депозит, чийто ключ е изведен от сървъра).
    [MaxLength(64)]
    [Required]
    public string RequestHash { get; set; } = string.Empty;
}
