import { Eye } from "lucide-react";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { AccountStatusBadge, EntityGrid } from "@/shared/components";
import type { CustomerDetails } from "@/types";

type CustomerAccountsTableProps = {
  accounts: CustomerDetails["accounts"];
  onOpen: (accountId: number) => void;
};

export default function CustomerAccountsTable({ accounts, onOpen }: CustomerAccountsTableProps) {
  if (accounts.length === 0) {
    return <p className="text-sm text-secondary">Няма сметки за този клиент.</p>;
  }

  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">IBAN</th>
          <th className="px-4 py-3">Салдо</th>
          <th className="px-4 py-3">Статус</th>
          <th className="px-4 py-3">Открита на</th>
          <th className="px-4 py-3 text-right">Действие</th>
        </tr>
      </thead>
      <tbody>
        {accounts.map((account) => (
          <tr key={account.id} className="border-b border-slate-100 text-sm last:border-b-0">
            <td className="px-4 py-3 font-mono text-xs">{account.iban}</td>
            <td className="px-4 py-3">{formatCurrency(account.balance)}</td>
            <td className="px-4 py-3">
              <AccountStatusBadge status={account.status} />
            </td>
            <td className="px-4 py-3">{formatDate(account.openedAtUtc)}</td>
            <td className="px-4 py-3 text-right">
              <button
                type="button"
                onClick={() => onOpen(account.id)}
                className="bank-secondary-btn bank-btn-action"
              >
                <Eye className="h-3.5 w-3.5" />
                Отвори
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </EntityGrid>
  );
}
