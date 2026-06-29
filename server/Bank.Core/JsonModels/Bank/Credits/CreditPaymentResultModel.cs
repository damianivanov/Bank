using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

/// <summary>
/// Резултат от регистриране на едно погасяване (вноска). Плащането на една вноска променя
/// само този ред плюс (при последното плащане) статуса на кредита, затова отговорът носи само
/// обновената вноска и състоянието на кредита вместо целия погасителен план — 30-годишен ипотечен
/// кредит иначе би пращал по 360 реда при всяко плащане.
/// </summary>
public class CreditPaymentResultModel
{
    public long CreditId { get; set; }
    public CreditStatus CreditStatus { get; set; }
    public DateTime? CreditRepaidAtUtc { get; set; }
    public CreditPaymentModel Payment { get; set; } = new();
}
