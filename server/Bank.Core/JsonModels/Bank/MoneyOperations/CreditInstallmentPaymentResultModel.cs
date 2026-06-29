using Bank.Core.Enums;
using Bank.Core.JsonModels.Bank.Credits;

namespace Bank.Core.JsonModels.Bank.MoneyOperations;

/// <summary>
/// Резултат от плащане на месечна вноска по кредит от салдото на сметка: платената вноска и статусът на
/// кредита (както при back-office плащането), плюс сметката-източник, новото ѝ салдо и записаното движение.
/// </summary>
public class CreditInstallmentPaymentResultModel
{
    public long CreditId { get; set; }
    public CreditStatus CreditStatus { get; set; }
    public DateTime? CreditRepaidAtUtc { get; set; }
    public CreditPaymentModel Payment { get; set; } = new();

    public long AccountId { get; set; }
    public string AccountIban { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public MoneyTransactionModel Transaction { get; set; } = new();
}
