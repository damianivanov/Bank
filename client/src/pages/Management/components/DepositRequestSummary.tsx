import { formatCurrency } from "@/lib/formatters";
import { AccountIbanCard } from "@/shared/components";
import type { DepositRequestQueue } from "@/types";

type DepositRequestSummaryProps = {
  request: DepositRequestQueue;
};

export default function DepositRequestSummary({ request }: DepositRequestSummaryProps) {
  return (
    <div className="space-y-3">
      <AccountIbanCard iban={request.accountIban} />
      <div className="flex items-center justify-between gap-3 rounded-xl border border-black/10 bg-black/[0.02] px-3.5 py-3 dark:border-white/10 dark:bg-white/[0.03]">
        <div className="min-w-0">
          <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Клиент</p>
          <p className="mt-0.5 truncate text-sm font-semibold">{request.customerDisplayName}</p>
        </div>
        <div className="shrink-0 text-right">
          <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Сума</p>
          <p className="mt-0.5 text-base font-bold tabular-nums text-accent">{formatCurrency(request.amount)}</p>
        </div>
      </div>
    </div>
  );
}
