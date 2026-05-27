import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { accountService } from "@/services/accountService";
import { AccountStatusBadge, EntityGrid } from "@/shared/components";
import type { BankAccount } from "@/types";

export default function AccountsList() {
  const [accounts, setAccounts] = useState<BankAccount[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadAccounts = async () => {
    setIsLoading(true);

    try {
      const accountsData = await accountService.getAccounts();
      setAccounts(accountsData);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load accounts"));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadAccounts();
  }, []);

  const renderContent = () => {
    if (isLoading) {
      return <p className="text-sm text-secondary">Loading accounts...</p>;
    }

    if (accounts.length === 0) {
      return <p className="text-sm text-secondary">No accounts yet.</p>;
    }

    return (
      <EntityGrid>
        <thead>
          <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
            <th className="px-4 py-3">IBAN</th>
            <th className="px-4 py-3">Customer</th>
            <th className="px-4 py-3">Balance</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Opened</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody>
          {accounts.map((account) => (
            <tr key={account.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3 font-mono text-xs">{account.iban}</td>
              <td className="px-4 py-3 font-semibold">{account.customerDisplayName}</td>
              <td className="px-4 py-3">{formatCurrency(account.balance)}</td>
              <td className="px-4 py-3">
                <AccountStatusBadge status={account.status} />
              </td>
              <td className="px-4 py-3">{formatDate(account.openedAtUtc)}</td>
              <td className="px-4 py-3 text-right">
                <Link to={`/accounts/${account.id}`} className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
                  Open
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </EntityGrid>
    );
  };

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Accounts</h1>
          <p className="mt-1 text-sm text-secondary">Open and monitor client bank accounts.</p>
        </div>
        <Link to="/accounts/new" className="bank-primary-btn rounded-xl px-4 py-2 text-sm font-semibold">
          Open account
        </Link>
      </div>

      <div className="mt-6">{renderContent()}</div>
    </section>
  );
}

