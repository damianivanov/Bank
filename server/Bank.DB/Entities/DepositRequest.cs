using System.ComponentModel.DataAnnotations;
using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

/// <summary>
/// Заявка за депозит, която чака одобрение от служител. Депозитът НЕ променя салдото при създаване —
/// салдото се кредитира едва когато служител одобри заявката. Така клиент не може директно да си "добави"
/// произволна сума. <see cref="CreatedById"/> е клиентът-заявител; <see cref="ReviewedById"/> е служителят.
/// </summary>
public class DepositRequest : BaseTrackUserEntity
{
    public long BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public DepositRequestStatus Status { get; set; }

    public long? ReviewedById { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    [MaxLength(500)]
    public string? ReviewNote { get; set; }

    // Прави повторното изпращане (двоен клик / retry) идемпотентно: едно и също намерение -> една заявка.
    [MaxLength(80)]
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    // Отпечатък (SHA-256) на бизнес-входа на заявката (сметка + сума). Същ ключ + друг отпечатък = опит за
    // подмяна на тялото при повторно изпращане -> 409 вместо тих replay.
    [MaxLength(64)]
    [Required]
    public string RequestHash { get; set; } = string.Empty;

    // Optimistic concurrency: спира двама служители да одобрят/отхвърлят една и съща заявка едновременно.
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
