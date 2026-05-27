import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { creditService } from "@/services/creditService";
import { CreditStatusBadge, EntityGrid, VipBadge } from "@/shared/components";
import type { Credit } from "@/types";

export default function CreditsList() {
  const [credits, setCredits] = useState<Credit[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadCredits = async () => {
    setIsLoading(true);

    try {
      const creditsData = await creditService.getCredits();
      setCredits(creditsData);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load credits"));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadCredits();
  }, []);

  const renderContent = () => {
    if (isLoading) {
      return <p className="text-sm text-secondary">Loading credits...</p>;
    }

    if (credits.length === 0) {
      return <p className="text-sm text-secondary">No credits yet.</p>;
    }

    return (
      <EntityGrid>
        <thead>
          <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
            <th className="px-4 py-3">Customer</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Amount</th>
            <th className="px-4 py-3">Rate</th>
            <th className="px-4 py-3">VIP at Creation</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Granted</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody>
          {credits.map((credit) => (
            <tr key={credit.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3 font-semibold">{credit.customerDisplayName}</td>
              <td className="px-4 py-3">{credit.creditType === 1 ? "Consumer" : "Mortgage"}</td>
              <td className="px-4 py-3">{formatCurrency(credit.grantedAmount)}</td>
              <td className="px-4 py-3">{formatPercent(credit.appliedAnnualInterestRate)}</td>
              <td className="px-4 py-3">
                <VipBadge isVip={credit.customerWasVipAtCreation} />
              </td>
              <td className="px-4 py-3">
                <CreditStatusBadge status={credit.status} />
              </td>
              <td className="px-4 py-3">{formatDate(credit.grantedAtUtc)}</td>
              <td className="px-4 py-3 text-right">
                <Link to={`/credits/${credit.id}`} className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
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
          <h1 className="text-3xl font-bold tracking-tight">Credits</h1>
          <p className="mt-1 text-sm text-secondary">Grant and track consumer and mortgage credits.</p>
        </div>
        <Link to="/credits/new" className="bank-primary-btn rounded-xl px-4 py-2 text-sm font-semibold">
          Grant credit
        </Link>
      </div>

      <div className="mt-6">{renderContent()}</div>
    </section>
  );
}

