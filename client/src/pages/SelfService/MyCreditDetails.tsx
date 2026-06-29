import { ArrowLeft, Banknote, CreditCard } from "lucide-react";
import { Link } from "react-router-dom";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { CreditRepaymentPlanTable } from "@/pages/Credits";
import { CreditStatusBadge, DetailField, PageBody, VipBadge } from "@/shared/components";
import { CreditPaymentStatus, CreditType } from "@/types";
import PayInstallmentModal from "./components/PayInstallmentModal";
import { useMyCreditDetailsPage } from "./hooks/useMyCreditDetailsPage";

export default function MyCreditDetails() {
  const { state, actions } = useMyCreditDetailsPage();

  if (state.isLoading || !state.credit) {
    return (
      <PageBody>
        <p className="text-sm text-secondary">Зареждане на кредита...</p>
      </PageBody>
    );
  }

  const credit = state.credit;
  const creditTypeLabel = credit.creditType === CreditType.Consumer ? "Потребителски" : "Ипотечен";
  const nextPendingPayment = credit.payments.find((p) => p.status === CreditPaymentStatus.Pending) ?? null;

  // Изплатена сума = сборът на вече платените вноски; прогресът е спрямо целия погасителен план.
  const paidInstallments = credit.payments.filter((p) => p.status === CreditPaymentStatus.Paid);
  const totalPaid = paidInstallments.reduce((sum, p) => sum + p.paymentAmount, 0);
  const totalScheduled = credit.payments.reduce((sum, p) => sum + p.paymentAmount, 0);
  const repaymentProgress = totalScheduled > 0 ? Math.round((totalPaid / totalScheduled) * 100) : 0;

  return (
    <PageBody>
      {/* Заглавие + връщане към прегледа; плащането живее долу, до самата вноска. */}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-3xl font-bold tracking-tight">Детайли за кредита</h1>
          <p className="mt-1 text-sm text-secondary">{creditTypeLabel} кредит и погасителен план.</p>
        </div>
        <Link to="/my-banking" className="bank-secondary-btn bank-btn">
          <ArrowLeft className="h-4 w-4" />
          Назад към прегледа
        </Link>
      </div>

      <section className="bank-panel mt-6 overflow-hidden rounded-2xl">
        {/* Идентичност: вид кредит + статус */}
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-black/10 p-5 sm:p-6 dark:border-white/10">
          <div className="flex min-w-0 items-center gap-3.5">
            <span className="bank-icon-tile-soft flex h-11 w-11 shrink-0 items-center justify-center rounded-xl">
              <Banknote className="h-5 w-5" />
            </span>
            <div className="min-w-0">
              <p className="truncate text-lg font-bold tracking-tight">{creditTypeLabel} кредит</p>
              <p className="text-sm text-secondary">Кредит № {credit.id}</p>
            </div>
          </div>
          <CreditStatusBadge status={credit.status} />
        </div>

        {/* Двоен герой: отпусната сума + месечна вноска с бутона за плащане */}
        <div className="grid gap-6 p-5 sm:grid-cols-2 sm:p-6">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Отпусната сума</p>
            <p className="mt-1.5 text-3xl font-bold tracking-tight tabular-nums text-accent sm:text-4xl">
              {formatCurrency(credit.grantedAmount)}
            </p>
            <p className="mt-1 text-sm tabular-nums text-secondary">за {credit.termMonths} месеца</p>
          </div>
          <div className="flex flex-col sm:border-l sm:border-black/10 sm:pl-6 dark:sm:border-white/10">
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Месечна вноска</p>
            <p className="mt-1.5 text-3xl font-bold tracking-tight tabular-nums sm:text-4xl">
              {formatCurrency(credit.plannedMonthlyPaymentAmount)}
            </p>
            {state.canPayInstallment ? (
              <>
                {nextPendingPayment ? (
                  <p className="mt-1 text-sm tabular-nums text-secondary">
                    Падеж на следващата вноска: {formatDate(nextPendingPayment.dueDate)}
                  </p>
                ) : null}
                <button
                  type="button"
                  onClick={actions.openPayModal}
                  className="bank-primary-btn bank-btn mt-3 w-full sm:w-auto sm:self-start"
                >
                  <CreditCard className="h-4 w-4" />
                  Плати следващата вноска
                </button>
              </>
            ) : nextPendingPayment ? (
              <p className="mt-1 text-sm tabular-nums text-secondary">
                Следваща вноска на {formatDate(nextPendingPayment.dueDate)}
              </p>
            ) : (
              <p className="mt-1 text-sm text-secondary">Всички вноски са платени.</p>
            )}
          </div>
        </div>

        {/* Прогрес по погасяване: изплатена сума спрямо целия план */}
        <div className="border-t border-black/10 p-5 sm:p-6 dark:border-white/10">
          <div className="flex flex-wrap items-end justify-between gap-x-6 gap-y-1">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Изплатена сума</p>
              <p className="mt-1.5 text-2xl font-bold tracking-tight tabular-nums text-accent sm:text-3xl">
                {formatCurrency(totalPaid)}
              </p>
            </div>
            <p className="text-sm tabular-nums text-secondary">
              {paidInstallments.length} от {credit.payments.length} платени вноски · {repaymentProgress}%
            </p>
          </div>
          <div
            className="mt-3 h-2 w-full overflow-hidden rounded-full bg-black/10 dark:bg-white/10"
            role="progressbar"
            aria-valuenow={repaymentProgress}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label="Прогрес по погасяване"
          >
            <div
              className="h-full rounded-full"
              style={{ width: `${repaymentProgress}%`, backgroundColor: "var(--accent)" }}
            />
          </div>
        </div>

        {/* Метаданни: по-едър текст за по-лесно четене на клиентската страница */}
        <div className="grid gap-6 border-t border-black/10 p-5 sm:grid-cols-2 sm:p-6 lg:grid-cols-3 dark:border-white/10">
          <DetailField label="Приложен годишен лихвен процент" valueClassName="text-base">
            {formatPercent(credit.appliedAnnualInterestRate)}
          </DetailField>
          <DetailField label="Текущ годишен лихвен процент" valueClassName="text-base">
            {formatPercent(credit.currentAnnualInterestRate)}
          </DetailField>
          <DetailField label="Такса за отпускане" valueClassName="text-base">
            {formatCurrency(credit.appliedGrantingFee)}
          </DetailField>
          <DetailField label="VIP при отпускане" valueClassName="font-normal">
            <VipBadge isVip={credit.customerWasVipAtCreation} />
          </DetailField>
          <DetailField label="Отпуснат на" valueClassName="text-base">
            {formatDate(credit.grantedAtUtc)}
          </DetailField>
          <DetailField label="Погасен на" valueClassName="text-base">
            {formatDate(credit.repaidAtUtc)}
          </DetailField>
        </div>
      </section>

      <section className="mt-6">
        <h2 className="mb-3 text-xl font-bold tracking-tight">Погасителен план</h2>
        <CreditRepaymentPlanTable payments={credit.payments} />
      </section>

      <PayInstallmentModal
        isOpen={state.isPayModalOpen}
        credit={credit}
        onClose={actions.closePayModal}
        onPaid={actions.reload}
      />
    </PageBody>
  );
}
