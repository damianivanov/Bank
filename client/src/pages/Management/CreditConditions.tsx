import { useEffect, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { creditConditionService } from "@/services/creditConditionService";
import { EntityGrid } from "@/shared/components";
import type { CreditTypeCondition } from "@/types";

export default function CreditConditionsPage() {
  const [conditions, setConditions] = useState<CreditTypeCondition[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadConditions = async () => {
    setIsLoading(true);

    try {
      const conditionsData = await creditConditionService.getCreditConditions();
      setConditions(conditionsData);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load credit conditions"));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadConditions();
  }, []);

  return (
    <section className="mx-auto w-full max-w-5xl px-4 py-4 md:px-6 md:py-5">
      <h1 className="text-2xl font-bold tracking-tight md:text-3xl">Credit Conditions</h1>
      <p className="mt-0.5 text-sm text-secondary">Read-only standard and VIP terms for available credit products.</p>

      <div className="mt-4">
        {isLoading ? (
          <p className="text-sm text-secondary">Loading credit conditions...</p>
        ) : (
          <EntityGrid>
            <thead className="text-xs">
              <tr className="border-b border-slate-200 uppercase tracking-wide text-tertiary">
                <th className="px-3 py-2.5">Type</th>
                <th className="px-3 py-2.5">Standard rate</th>
                <th className="px-3 py-2.5">VIP rate</th>
                <th className="px-3 py-2.5">Max amount</th>
                <th className="px-3 py-2.5">Max term</th>
                <th className="px-3 py-2.5">Standard fee</th>
                <th className="px-3 py-2.5">VIP fee</th>
              </tr>
            </thead>
            <tbody>
              {conditions.map((condition) => (
                <tr key={condition.id} className="border-b border-slate-100 text-sm last:border-b-0">
                  <td className="px-3 py-2.5 font-semibold">{condition.name}</td>
                  <td className="px-3 py-2.5">{formatPercent(condition.standardAnnualInterestRate)}</td>
                  <td className="px-3 py-2.5">{formatPercent(condition.vipAnnualInterestRate)}</td>
                  <td className="px-3 py-2.5">{formatCurrency(condition.maximumAmount)}</td>
                  <td className="px-3 py-2.5">{condition.maximumTermMonths} months</td>
                  <td className="px-3 py-2.5">{formatCurrency(condition.standardGrantingFee)}</td>
                  <td className="px-3 py-2.5">{formatCurrency(condition.vipGrantingFee)}</td>
                </tr>
              ))}
            </tbody>
          </EntityGrid>
        )}
      </div>
    </section>
  );
}

