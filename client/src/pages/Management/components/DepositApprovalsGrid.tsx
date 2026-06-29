import { Check, X } from "lucide-react";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { DepositStatusBadge, EntityGrid } from "@/shared/components";
import { DepositRequestStatus, type DepositRequestQueue } from "@/types";

type DepositApprovalsGridProps = {
  requests: DepositRequestQueue[];
  processingId: number | null;
  onApprove: (request: DepositRequestQueue) => void;
  onReject: (request: DepositRequestQueue) => void;
};

export default function DepositApprovalsGrid({
  requests,
  processingId,
  onApprove,
  onReject,
}: DepositApprovalsGridProps) {
  if (requests.length === 0) {
    return <p className="text-sm text-secondary">Няма заявки за депозит за този филтър.</p>;
  }

  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">Подадена</th>
          <th className="px-4 py-3">Клиент</th>
          <th className="px-4 py-3">Сметка</th>
          <th className="px-4 py-3 text-right">Сума</th>
          <th className="px-4 py-3">Статус</th>
          <th className="px-4 py-3 text-right">Действия</th>
        </tr>
      </thead>
      <tbody>
        {requests.map((request) => {
          const isPending = request.status === DepositRequestStatus.Pending;
          const isProcessing = processingId === request.id;
          return (
            <tr key={request.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3">{formatDate(request.dateCreated)}</td>
              <td className="px-4 py-3 font-semibold">{request.customerDisplayName}</td>
              <td className="px-4 py-3 font-mono text-xs">{request.accountIban}</td>
              <td className="px-4 py-3 text-right font-semibold">{formatCurrency(request.amount)}</td>
              <td className="px-4 py-3">
                <DepositStatusBadge status={request.status} />
              </td>
              <td className="px-4 py-3">
                {isPending ? (
                  <div className="flex flex-wrap items-center justify-end gap-2">
                    <button
                      type="button"
                      onClick={() => onApprove(request)}
                      disabled={isProcessing}
                      className="bank-primary-btn bank-btn-action disabled:opacity-50"
                    >
                      <Check className="h-3.5 w-3.5" />
                      {isProcessing ? "..." : "Одобри"}
                    </button>
                    <button
                      type="button"
                      onClick={() => onReject(request)}
                      disabled={isProcessing}
                      className="bank-secondary-btn bank-btn-action disabled:opacity-50"
                    >
                      <X className="h-3.5 w-3.5" />
                      Отхвърли
                    </button>
                  </div>
                ) : (
                  <p className="text-right text-xs text-tertiary">{request.reviewNote ?? "-"}</p>
                )}
              </td>
            </tr>
          );
        })}
      </tbody>
    </EntityGrid>
  );
}
