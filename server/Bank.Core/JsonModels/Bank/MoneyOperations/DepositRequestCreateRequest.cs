using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

public class DepositRequestCreateRequest
{
    [Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    // Клиентът генерира ключа (crypto.randomUUID) и го праща пак при retry, за да не се създаде дубликат.
    [Required]
    [StringLength(80, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;
}
