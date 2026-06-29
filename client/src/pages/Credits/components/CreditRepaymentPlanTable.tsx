import { formatCurrency, formatDate } from "@/lib/formatters";
import { EntityGrid, PaymentStatusBadge } from "@/shared/components";
import type { CreditDetails } from "@/types";

type CreditRepaymentPlanTableProps = {
  payments: CreditDetails["payments"];
};

export default function CreditRepaymentPlanTable({ payments }: CreditRepaymentPlanTableProps) {
  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">#</th>
          <th className="px-4 py-3">Падеж</th>
          <th className="px-4 py-3">Вноска</th>
          <th className="px-4 py-3">Главница</th>
          <th className="px-4 py-3">Лихва</th>
          <th className="px-4 py-3">Остатък главница</th>
          <th className="px-4 py-3">Статус</th>
          <th className="px-4 py-3">Платена на</th>
        </tr>
      </thead>
      <tbody>
        {payments.map((payment) => (
          <tr key={payment.id} className="border-b border-slate-100 text-sm last:border-b-0">
            <td className="px-4 py-3 font-semibold">{payment.paymentNumber}</td>
            <td className="px-4 py-3">{formatDate(payment.dueDate)}</td>
            <td className="px-4 py-3">{formatCurrency(payment.paymentAmount)}</td>
            <td className="px-4 py-3">{formatCurrency(payment.principalPart)}</td>
            <td className="px-4 py-3">{formatCurrency(payment.interestPart)}</td>
            <td className="px-4 py-3">{formatCurrency(payment.remainingPrincipalAfterPayment)}</td>
            <td className="px-4 py-3">
              <PaymentStatusBadge status={payment.status} />
            </td>
            <td className="px-4 py-3">{formatDate(payment.paidAtUtc)}</td>
          </tr>
        ))}
      </tbody>
    </EntityGrid>
  );
}
