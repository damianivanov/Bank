using Bank.Core.Enums;

namespace Bank.Core.JsonModels.Bank.Credits;

public class CreditDetailsModel
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public CreditType CreditType { get; set; }
    public decimal GrantedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal AppliedAnnualInterestRate { get; set; }
    public decimal AppliedGrantingFee { get; set; }
    public bool CustomerWasVipAtCreation { get; set; }
    public decimal PlannedMonthlyPaymentAmount { get; set; }
    public decimal CurrentAnnualInterestRate { get; set; }

    public decimal TotalInterest { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalAmountWithFees { get; set; }
    public CreditStatus Status { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public DateTime? RepaidAtUtc { get; set; }
    public CreditPricingChangeModel? LastPricingChange { get; set; }
    public CreditTermsModel? CurrentTerms { get; set; }

    // Пълна хронология на промените по кредита, така както е записана в базата:
    // всяка версия на условията (CreditTerms + CreditTermsFee) и всяко ценово събитие (CreditPricingChange).
    // Подреждани са от най-новата към най-старата. Само-обслужващият UI ги показва в информационен прозорец.
    public IReadOnlyCollection<CreditTermsHistoryModel> TermsHistory { get; set; } = [];
    public IReadOnlyCollection<CreditPricingChangeModel> PricingChanges { get; set; } = [];

    // Може ли клиентът да плати следващата вноска сега (активен кредит + настъпил падеж; в Development
    // може да е разрешено и предсрочно плащане). Само-обслужващият UI показва бутона за плащане по тази стойност.
    public bool CanPayNextInstallment { get; set; }
    public IReadOnlyCollection<CreditPaymentModel> Payments { get; set; } = [];
}
