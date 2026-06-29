import { Link } from "react-router-dom";
import { ArrowLeft, Banknote } from "lucide-react";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { CreditStatusBadge, DetailField, PageBody, PageHeader, VipBadge } from "@/shared/components";
import CreditRepaymentPlanTable from "./components/CreditRepaymentPlanTable";
import { feeKindLabels, formatCreditType, formatFeeValue, paymentTypeLabels } from "./utils/creditDisplay";
import { useCreditDetailsPage } from "./hooks/useCreditDetailsPage";

export default function CreditDetails() {
  const { state } = useCreditDetailsPage();

  if (state.isLoading || !state.credit) {
    return (
      <PageBody>
        <p className="text-sm text-secondary">Зареждане на кредит...</p>
      </PageBody>
    );
  }

  const credit = state.credit;

  return (
    <PageBody>
      <div className="mb-4">
        <Link to="/credits" className="bank-secondary-btn bank-btn">
          <ArrowLeft className="h-4 w-4" />
          Назад
        </Link>
      </div>

      <PageHeader title="Детайли за кредит" />

      <section className="bank-panel mt-6 overflow-hidden rounded-2xl">
        {/* Идентичност: вид кредит + клиент + статус */}
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-black/10 p-5 sm:p-6 dark:border-white/10">
          <div className="flex min-w-0 items-center gap-3.5">
            <span className="bank-icon-tile-soft flex h-11 w-11 shrink-0 items-center justify-center rounded-xl">
              <Banknote className="h-5 w-5" />
            </span>
            <div className="min-w-0">
              <p className="truncate text-base font-bold tracking-tight sm:text-lg">{formatCreditType(credit.creditType)}</p>
              <Link to={`/customers/${credit.customerId}`} className="bank-accent-link text-sm font-medium">
                {credit.customerDisplayName}
              </Link>
            </div>
          </div>
          <CreditStatusBadge status={credit.status} />
        </div>

        {/* Двоен герой: отпусната сума + месечна вноска */}
        <div className="grid gap-5 p-5 sm:grid-cols-2 sm:p-6">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Отпусната сума</p>
            <p className="mt-1 text-3xl font-bold tracking-tight tabular-nums text-accent sm:text-4xl">
              {formatCurrency(credit.grantedAmount)}
            </p>
          </div>
          <div className="sm:border-l sm:border-black/10 sm:pl-5 dark:sm:border-white/10">
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Планирана вноска / месец</p>
            <p className="mt-1 text-2xl font-bold tracking-tight tabular-nums sm:text-3xl">
              {formatCurrency(credit.plannedMonthlyPaymentAmount)}
            </p>
            <p className="mt-0.5 text-xs tabular-nums text-tertiary">за {credit.termMonths} месеца</p>
          </div>
        </div>

        {/* Метаданни */}
        <div className="grid gap-5 border-t border-black/10 p-5 sm:grid-cols-2 sm:p-6 lg:grid-cols-3 dark:border-white/10">
          <DetailField label="Приложен годишен лихвен процент">{formatPercent(credit.appliedAnnualInterestRate)}</DetailField>
          <DetailField label="Текущ годишен лихвен процент">{formatPercent(credit.currentAnnualInterestRate)}</DetailField>
          <DetailField label="Такса за отпускане">{formatCurrency(credit.appliedGrantingFee)}</DetailField>
          <DetailField label="VIP при създаване" valueClassName="font-normal">
            <VipBadge isVip={credit.customerWasVipAtCreation} />
          </DetailField>
          <DetailField label="Отпуснат на">{formatDate(credit.grantedAtUtc)}</DetailField>
          <DetailField label="Погасен на">{formatDate(credit.repaidAtUtc)}</DetailField>
        </div>
      </section>

      {credit.currentTerms ? (
        <section className="bank-panel mt-6 rounded-2xl p-5 sm:p-6">
          <h2 className="text-lg font-bold tracking-tight">Условия по кредита</h2>
          <div className="mt-4 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            <DetailField label="ГПР">{formatPercent(credit.currentTerms.apr)}</DetailField>
            <DetailField label="Погасителен план">{paymentTypeLabels[credit.currentTerms.paymentType]}</DetailField>
            {credit.currentTerms.promoPeriodMonths > 0 ? (
              <DetailField label="Промоционален период">
                {credit.currentTerms.promoPeriodMonths} мес.
                {credit.currentTerms.promoAnnualInterestRate != null
                  ? ` · ${formatPercent(credit.currentTerms.promoAnnualInterestRate)}`
                  : ""}
              </DetailField>
            ) : null}
            {credit.currentTerms.gracePeriodMonths > 0 ? (
              <DetailField label="Гратисен период">{credit.currentTerms.gracePeriodMonths} мес.</DetailField>
            ) : null}
          </div>
          {credit.currentTerms.fees.length > 0 ? (
            <div className="mt-5">
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Такси</p>
              <ul className="mt-2 grid gap-2 sm:grid-cols-2">
                {credit.currentTerms.fees.map((fee) => (
                  <li
                    key={fee.kind}
                    className="flex items-center justify-between rounded-xl border border-black/10 px-3 py-2 text-sm dark:border-white/10"
                  >
                    <span className="text-secondary">{feeKindLabels[fee.kind]}</span>
                    <span className="font-semibold tabular-nums">{formatFeeValue(fee)}</span>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </section>
      ) : null}

      {credit.lastPricingChange ? (
        <section className="bank-panel mt-6 rounded-2xl p-5 sm:p-6">
          <h2 className="text-lg font-bold tracking-tight">Последна промяна в ценообразуването</h2>
          <div className="mt-4 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            <DetailField label="Предишен лихвен процент">
              {formatPercent(credit.lastPricingChange.previousAnnualInterestRate)}
            </DetailField>
            <DetailField label="Нов лихвен процент">
              {formatPercent(credit.lastPricingChange.newAnnualInterestRate)}
            </DetailField>
            <DetailField label="В сила от вноска">#{credit.lastPricingChange.effectiveFromPaymentNumber}</DetailField>
            <DetailField label="Променен на">{formatDate(credit.lastPricingChange.dateCreated)}</DetailField>
          </div>
        </section>
      ) : null}

      <section className="mt-6">
        <h2 className="mb-3 text-xl font-bold tracking-tight">Погасителен план</h2>
        <CreditRepaymentPlanTable payments={credit.payments} />
      </section>
    </PageBody>
  );
}
