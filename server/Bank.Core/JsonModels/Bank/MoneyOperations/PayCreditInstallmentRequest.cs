using System.ComponentModel.DataAnnotations;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

public class PayCreditInstallmentRequest
{
    // По избор: коя сметка да финансира вноската. Ако е null и клиентът има точно една активна сметка,
    // тя се ползва автоматично; при няколко сметки сървърът връща грешка, за да накара клиента да избере.
    [Range(1, long.MaxValue)]
    public long? FundingAccountId { get; set; }

    [Required]
    [StringLength(80, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;
}
