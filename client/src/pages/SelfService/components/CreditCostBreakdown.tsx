import { History } from "lucide-react";
import { feeKindLabels, formatFeeValue, paymentTypeLabels } from "@/pages/Credits";
import { CollapsibleSection } from "@/shared/components";
import { type CreditDetails } from "@/types";
import { formatCurrency, formatPercent } from "@/lib/formatters";

type CreditCostBreakdownProps = {
  credit: CreditDetails;
  onShowChanges: () => void;
};

// Цветовете на сегментите идват от темата, за да са в синхрон със светъл/тъмен режим.
const SEGMENTS = [
  { key: "principal", label: "Главница", color: "var(--accent)" },
  { key: "interest", label: "Лихва", color: "var(--blue)" },
  { key: "fees", label: "Такси", color: "var(--amber)" },
] as const;

export default function CreditCostBreakdown({ credit, onShowChanges }: CreditCostBreakdownProps) {
  const terms = credit.currentTerms;

  // Обща дължима сума = Главница + Лихва + Такси. Стойностите идват от запазения план, затова сумата им съвпада.
  const segments = [
    { ...SEGMENTS[0], value: credit.grantedAmount },
    { ...SEGMENTS[1], value: credit.totalInterest },
    { ...SEGMENTS[2], value: credit.totalFees },
  ];
  const total = segments.reduce((sum, segment) => sum + segment.value, 0);
  const safeTotal = total > 0 ? total : 1;

  return (
    <CollapsibleSection
      title="Разбивка на разходите"
      description="ГПР, обща дължима сума, лихви и такси по кредита"
      headerAction={
        (credit.termsHistory?.length ?? 0) > 0 ? (
          <button
            type="button"
            onClick={onShowChanges}
            className="bank-secondary-btn inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold"
          >
            <History className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Промени по кредита</span>
            <span className="sm:hidden">Промени</span>
          </button>
        ) : undefined
      }
    >
      {/* Заглавна стойност — най-важното число за кредитополучателя */}
      <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Обща дължима сума</p>
      <p className="mt-1 text-3xl font-bold tracking-tight tabular-nums sm:text-4xl">
        {formatCurrency(credit.totalAmountWithFees)}
      </p>

      {/* Лента на разбивката — пропорция между главница, лихва и такси */}
      <div className="mt-4 h-2.5 w-full overflow-hidden rounded-full bg-black/5 dark:bg-white/10">
        <div className="bank-reveal-x flex h-full w-full">
          {segments.map((segment) =>
            segment.value > 0 ? (
              <div
                key={segment.key}
                className="h-full"
                style={{ width: `${(segment.value / safeTotal) * 100}%`, minWidth: 3, backgroundColor: segment.color }}
              />
            ) : null,
          )}
        </div>
      </div>

      {/* Легенда със стойностите на компонентите */}
      <ul className="mt-3.5 grid gap-x-6 gap-y-2 sm:grid-cols-3">
        {segments.map((segment) => (
          <li
            key={segment.key}
            className="flex items-center justify-between gap-3 sm:flex-col sm:items-start sm:gap-1"
          >
            <span className="flex items-center gap-2 text-sm text-secondary">
              <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: segment.color }} aria-hidden />
              {segment.label}
            </span>
            <span className="text-sm font-semibold tabular-nums">{formatCurrency(segment.value)}</span>
          </li>
        ))}
      </ul>

      {/* Вторични метрики */}
      {terms ? (
        <div className="mt-5 flex flex-wrap gap-2 border-t border-black/5 pt-4 dark:border-white/10">
          <MetaChip label="ГПР" value={formatPercent(terms.apr)} tone="accent" />
          <MetaChip label="Погасителен план" value={paymentTypeLabels[terms.paymentType]} />
          {terms.promoPeriodMonths > 0 ? (
            <MetaChip
              label="Промо период"
              value={`${terms.promoPeriodMonths} мес.${
                terms.promoAnnualInterestRate != null ? ` · ${formatPercent(terms.promoAnnualInterestRate)}` : ""
              }`}
            />
          ) : null}
          {terms.gracePeriodMonths > 0 ? (
            <MetaChip label="Гратисен период" value={`${terms.gracePeriodMonths} мес.`} />
          ) : null}
        </div>
      ) : null}

      {/* Разбивка на отделните такси */}
      {terms && terms.fees.length > 0 ? (
        <div className="mt-4">
          <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Такси</p>
          <ul className="mt-2 grid gap-2 sm:grid-cols-2">
            {terms.fees.map((fee) => (
              <li
                key={fee.kind}
                className="flex items-center justify-between gap-3 rounded-xl border border-black/10 px-3 py-2 text-sm dark:border-white/10"
              >
                <span className="text-secondary">{feeKindLabels[fee.kind]}</span>
                <span className="font-semibold tabular-nums">{formatFeeValue(fee)}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </CollapsibleSection>
  );
}

function MetaChip({ label, value, tone = "neutral" }: { label: string; value: string; tone?: "neutral" | "accent" }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs ${
        tone === "accent" ? "bank-accent-pill" : "bank-chip"
      }`}
    >
      <span className="font-semibold uppercase tracking-wide opacity-70">{label}</span>
      <span className="font-semibold tabular-nums">{value}</span>
    </span>
  );
}
