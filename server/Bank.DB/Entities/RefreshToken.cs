using Bank.DB.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Bank.DB.Entities;

public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }

    [MaxLength(2048)]
    public string Value { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public User User { get; set; } = null!;
}
