using System.ComponentModel.DataAnnotations;
using Bank.Core.Enums;
using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB.Entities;

public class BankAccount : BaseTrackUserEntity
{
    [MaxLength(34)]
    [Required]
    public string IBAN { get; set; } = string.Empty;
    
    [Precision(18, 2)]
    public decimal Balance { get; set; } = 0.0m;

    [Required]
    public BankAccountStatus Status { get; set; }
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required]
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    // Optimistic concurrency token. Без него едновременни тегления биха могли всяко да прочете старото
    // салдо и да го презапишат (lost update / double spend). При конфликт SaveChanges хвърля
    // DbUpdateConcurrencyException и операцията се преопитва с прясно салдо. Виж MoneyOperationService.
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
