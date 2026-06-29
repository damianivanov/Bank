import { formatCurrency, formatDate } from "@/lib/formatters";
import { DepositStatusBadge, EntityGrid } from "@/shared/components";
import { useMyDepositRequests } from "../hooks/useMyDepositRequests";

type MyDepositRequestsPanelProps = {
  refreshSignal: number;
  customerId: number | null;
};

export default function MyDepositRequestsPanel({ refreshSignal, customerId }: MyDepositRequestsPanelProps) {
  const { state } = useMyDepositRequests(refreshSignal, customerId);

  if (state.isLoading) {
    return <p className="text-sm text-secondary">Зареждане на заявките за депозит...</p>;
  }

  if (state.error) {
    return <p className="text-sm font-semibold text-rose-500">{state.error}</p>;
  }

  if (state.requests.length === 0) {
    return <p className="text-sm text-secondary">Нямате заявки за депозит.</p>;
  }

  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">Подадена</th>
          <th className="px-4 py-3">Сметка</th>
          <th className="px-4 py-3 text-right">Сума</th>
          <th className="px-4 py-3">Статус</th>
          <th className="px-4 py-3">Бележка</th>
        </tr>
      </thead>
      <tbody>
        {state.requests.map((request) => (
          <tr key={request.id} className="border-b border-slate-100 text-sm last:border-b-0">
            <td className="px-4 py-3">{formatDate(request.dateCreated)}</td>
            <td className="px-4 py-3 font-mono text-xs">{request.accountIban}</td>
            <td className="px-4 py-3 text-right font-semibold">{formatCurrency(request.amount)}</td>
            <td className="px-4 py-3">
              <DepositStatusBadge status={request.status} />
            </td>
            <td className="px-4 py-3 text-secondary">{request.reviewNote ?? "-"}</td>
          </tr>
        ))}
      </tbody>
    </EntityGrid>
  );
}
