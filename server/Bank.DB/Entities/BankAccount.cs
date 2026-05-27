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
}
