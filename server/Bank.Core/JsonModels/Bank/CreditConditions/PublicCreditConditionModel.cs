using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.CreditConditions;

// Публичен изглед на кредитните условия за калкулатора (достъпен анонимно). Съдържа стандартните
// стойности, с които калкулаторът предзарежда формата (процент, лимити, промо/гратисен период,
// погасителен план и стандартните такси); VIP условията остават само за служители.
public class PublicCreditConditionModel
{
    public long Id { get; set; }
    public CreditType CreditType { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal StandardAnnualInterestRate { get; set; }
    public decimal MaximumAmount { get; set; }
    public int MaximumTermMonths { get; set; }
    public PaymentType DefaultPaymentType { get; set; }
    public int PromoPeriodMonths { get; set; }
    public decimal? StandardPromoRate { get; set; }
    public int GracePeriodMonths { get; set; }
    public decimal StandardGrantingFee { get; set; }
    public decimal StandardMonthlyManagementFee { get; set; }
    public decimal StandardAnnualManagementFee { get; set; }
}
