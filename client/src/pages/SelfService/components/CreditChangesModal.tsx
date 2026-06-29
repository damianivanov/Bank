import { type ReactNode } from "react";
import { ArrowRight } from "lucide-react";
import {
  creditTermsOriginLabels,
  feeKindLabels,
  formatFeeValue,
  paymentTypeLabels,
  pricingChangeReasonLabels,
} from "@/pages/Credits";
import { Modal, VipBadge } from "@/shared/components";
import { CreditTermsOrigin, type CreditPricingChange, type CreditTermsHistory } from "@/types";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";

type CreditChangesModalProps = {
  isOpen: boolean;
  onClose: () => void;
  termsHistory: CreditTermsHistory[];
  pricingChanges: CreditPricingChange[];
};

// Една версия от условията + (ако има) свързаната ценова промяна за същата вноска.
// Всичко идва директно от CreditTerms / CreditTermsFee / CreditPricingChange — без изчисления.
export default function CreditChangesModal({ isOpen, onClose, termsHistory, pricingChanges }: CreditChangesModalProps) {
  // termsHistory е подреден от най-новата към най-старата версия — следващият елемент е предишната по време версия.
  const hasOnlyOrigination = termsHistory.length <= 1;

  return (
    <Modal title="Промени по кредита" isOpen={isOpen} onClose={onClose} widthClassName="max-w-2xl">
      <p className="-mt-2 mb-4 text-sm text-secondary">
        Хронология на условията и ценообразуването — от най-новата промяна към отпускането.
      </p>

      {hasOnlyOrigination ? (
        <p className="mb-4 rounded-xl border border-black/10 bg-black/[0.02] px-4 py-3 text-sm text-secondary dark:border-white/10 dark:bg-white/[0.03]">
          Към момента няма промени по условията след отпускането на кредита.
        </p>
      ) : null}

      <ol className="space-y-5">
        {termsHistory.map((terms, index) => {
          const previous = termsHistory[index + 1];
          const isRepricing = terms.origin === CreditTermsOrigin.VipRepricing;
          // Свързваме ценовото събитие само към репрайсинг версия и то по вноска + нов лихвен процент,
          // за да не „залепне“ промяна към първоначалните условия, ако репрайсинг е влязъл още от вноска №1.
          const pricingChange = isRepricing
            ? pricingChanges.find(
                (change) =>
                  change.effectiveFromPaymentNumber === terms.effectiveFromPaymentNumber &&
                  change.newAnnualInterestRate === terms.baseAnnualInterestRate,
              )
            : undefined;
          const vipChanged = previous != null && previous.wasVipApplied !== terms.wasVipApplied;

          return (
            <li
              key={`${terms.effectiveFromPaymentNumber}-${terms.dateCreated}`}
              className="relative border-l border-black/10 pb-1 pl-5 dark:border-white/10"
            >
              <span
                className="absolute -left-[5px] top-1 h-2.5 w-2.5 rounded-full ring-4 ring-[var(--color-panel-strong)]"
                style={{ background: terms.isCurrent ? "var(--accent)" : "var(--text-tertiary)" }}
                aria-hidden
              />

              {/* Ред със заглавие: произход, маркер „текущи“, дата и вноска */}
              <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                <span
                  className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${
                    isRepricing ? "bank-chip-info" : "bank-chip"
                  }`}
                >
                  {creditTermsOriginLabels[terms.origin]}
                </span>
                {terms.isCurrent ? (
                  <span className="bank-accent-pill inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold">
                    Текущи
                  </span>
                ) : null}
                <span className="text-xs tabular-nums text-tertiary">
                  {formatDate(terms.dateCreated)} · в сила от вноска №{terms.effectiveFromPaymentNumber}
                </span>
              </div>

              {/* Фактите по версията */}
              <div className="mt-2.5 grid gap-x-6 gap-y-2.5 sm:grid-cols-2">
                <Fact label="Лихвен процент">
                  {pricingChange ? (
                    <span className="inline-flex items-center gap-1.5">
                      <span className="text-tertiary line-through">
                        {formatPercent(pricingChange.previousAnnualInterestRate)}
                      </span>
                      <ArrowRight className="h-3.5 w-3.5 text-tertiary" aria-hidden />
                      <span className="font-semibold text-accent">
                        {formatPercent(pricingChange.newAnnualInterestRate)}
                      </span>
                    </span>
                  ) : (
                    formatPercent(terms.baseAnnualInterestRate)
                  )}
                </Fact>

                <Fact label="ГПР">{formatPercent(terms.apr)}</Fact>

                <Fact label="VIP условия">
                  {vipChanged ? (
                    <span className="inline-flex items-center gap-1.5">
                      <VipBadge isVip={previous!.wasVipApplied} />
                      <ArrowRight className="h-3.5 w-3.5 text-tertiary" aria-hidden />
                      <VipBadge isVip={terms.wasVipApplied} />
                    </span>
                  ) : (
                    <VipBadge isVip={terms.wasVipApplied} />
                  )}
                </Fact>

                <Fact label="Планирана вноска">{formatCurrency(terms.plannedMonthlyPaymentAmount)}</Fact>

                <Fact label="Погасителен план">{paymentTypeLabels[terms.paymentType]}</Fact>

                {terms.promoPeriodMonths > 0 ? (
                  <Fact label="Промоционален период">
                    {terms.promoPeriodMonths} мес.
                    {terms.promoAnnualInterestRate != null ? ` · ${formatPercent(terms.promoAnnualInterestRate)}` : ""}
                  </Fact>
                ) : null}

                {terms.gracePeriodMonths > 0 ? (
                  <Fact label="Гратисен период">{terms.gracePeriodMonths} мес.</Fact>
                ) : null}

                {pricingChange ? <Fact label="Причина">{pricingChangeReasonLabels[pricingChange.reason]}</Fact> : null}
              </div>

              {/* Такси по версията */}
              {terms.fees.length > 0 ? (
                <div className="mt-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Такси</p>
                  <ul className="mt-1.5 grid gap-1.5 sm:grid-cols-2">
                    {terms.fees.map((fee) => (
                      <li
                        key={fee.kind}
                        className="flex items-center justify-between gap-3 rounded-lg border border-black/10 px-2.5 py-1.5 text-xs dark:border-white/10"
                      >
                        <span className="text-secondary">{feeKindLabels[fee.kind]}</span>
                        <span className="font-semibold tabular-nums">{formatFeeValue(fee)}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </li>
          );
        })}
      </ol>
    </Modal>
  );
}

function Fact({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 sm:flex-col sm:items-start sm:gap-1">
      <span className="text-xs font-semibold uppercase tracking-wide text-tertiary">{label}</span>
      <span className="text-sm font-medium tabular-nums">{children}</span>
    </div>
  );
}
