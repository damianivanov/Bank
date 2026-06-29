import { ArrowDownToLine, ArrowUpFromLine, History } from "lucide-react";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { AccountStatusBadge, EntityGrid } from "@/shared/components";
import { BankAccountStatus, type CustomerAccountSummary, type CustomerDetails } from "@/types";

type MyAccountsTableProps = {
  accounts: CustomerDetails["accounts"];
  onDeposit: (account: CustomerAccountSummary) => void;
  onWithdraw: (account: CustomerAccountSummary) => void;
  onViewHistory: (account: CustomerAccountSummary) => void;
};

export default function MyAccountsTable({ accounts, onDeposit, onWithdraw, onViewHistory }: MyAccountsTableProps) {
  if (accounts.length === 0) {
    return <p className="text-sm text-secondary">Все още нямате банкови сметки.</p>;
  }

  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">IBAN</th>
          <th className="px-4 py-3">Салдо</th>
          <th className="px-4 py-3">Статус</th>
          <th className="px-4 py-3">Открита</th>
          <th className="px-4 py-3 text-right">Действия</th>
        </tr>
      </thead>
      <tbody>
        {accounts.map((account) => {
          const isActive = account.status === BankAccountStatus.Active;
          return (
            <tr key={account.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3 font-mono text-xs">{account.iban}</td>
              <td className="px-4 py-3">{formatCurrency(account.balance)}</td>
              <td className="px-4 py-3">
                <AccountStatusBadge status={account.status} />
              </td>
              <td className="px-4 py-3">{formatDate(account.openedAtUtc)}</td>
              <td className="px-4 py-3">
                <div className="flex flex-wrap items-center justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => onDeposit(account)}
                    disabled={!isActive}
                    className="bank-primary-btn bank-btn-action disabled:opacity-50"
                  >
                    <ArrowDownToLine className="h-4 w-4" />
                    Депозит
                  </button>
                  <button
                    type="button"
                    onClick={() => onWithdraw(account)}
                    disabled={!isActive}
                    className="bank-secondary-btn bank-btn-action disabled:opacity-50"
                  >
                    <ArrowUpFromLine className="h-4 w-4" />
                    Теглене
                  </button>
                  <button
                    type="button"
                    onClick={() => onViewHistory(account)}
                    className="bank-secondary-btn bank-btn-action"
                  >
                    <History className="h-4 w-4" />
                    Движения
                  </button>
                </div>
              </td>
            </tr>
          );
        })}
      </tbody>
    </EntityGrid>
  );
}
