import { Eye } from "lucide-react";
import { Link } from "react-router-dom";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { CreditStatusBadge, EntityGrid } from "@/shared/components";
import { CreditType, type CustomerDetails } from "@/types";

type MyCreditsTableProps = {
  credits: CustomerDetails["credits"];
};

export default function MyCreditsTable({ credits }: MyCreditsTableProps) {
  if (credits.length === 0) {
    return <p className="text-sm text-secondary">Все още нямате кредити.</p>;
  }

  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">Тип</th>
          <th className="px-4 py-3">Сума</th>
          <th className="px-4 py-3">Срок</th>
          <th className="px-4 py-3">Лихва</th>
          <th className="px-4 py-3">Месечна вноска</th>
          <th className="px-4 py-3">Статус</th>
          <th className="px-4 py-3">Отпуснат</th>
          <th className="px-4 py-3 text-right">Действие</th>
        </tr>
      </thead>
      <tbody>
        {credits.map((credit) => (
          <tr key={credit.id} className="border-b border-slate-100 text-sm last:border-b-0">
            <td className="px-4 py-3">{credit.creditType === CreditType.Consumer ? "Потребителски" : "Ипотечен"}</td>
            <td className="px-4 py-3">{formatCurrency(credit.grantedAmount)}</td>
            <td className="px-4 py-3">{credit.termMonths} месеца</td>
            <td className="px-4 py-3">{formatPercent(credit.appliedAnnualInterestRate)}</td>
            <td className="px-4 py-3">{formatCurrency(credit.plannedMonthlyPaymentAmount)}</td>
            <td className="px-4 py-3">
              <CreditStatusBadge status={credit.status} />
            </td>
            <td className="px-4 py-3">{formatDate(credit.grantedAtUtc)}</td>
            <td className="px-4 py-3 text-right">
              <Link
                to={`/my-banking/credits/${credit.id}`}
                className="bank-secondary-btn bank-btn-action"
              >
                <Eye className="h-3.5 w-3.5" />
                Виж плана
              </Link>
            </td>
          </tr>
        ))}
      </tbody>
    </EntityGrid>
  );
}
