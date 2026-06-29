using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

public class WithdrawalCreateRequest
{
    [Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(80, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;
}
