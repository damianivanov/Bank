namespace Bank.Services.Credits;

/// <summary>
/// Правило кога вноска по кредит е платима от клиента (само-обслужване). Платима е, ако падежният ѝ месец
/// е настъпил — текущият календарен месец или по-ранен (просрочените също). Бъдещи вноски не са платими
/// предварително, освен ако dev променливата <c>allowFutureInstallments</c> е true.
/// </summary>
public static class InstallmentPaymentPolicy
{
    public static bool IsInstallmentPayable(DateTime dueDateUtc, DateTime nowUtc, bool allowFutureInstallments)
    {
        if (allowFutureInstallments)
        {
            return true;
        }

        var dueMonth = new DateTime(dueDateUtc.Year, dueDateUtc.Month, 1);
        var currentMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1);
        return dueMonth <= currentMonth;
    }
}
