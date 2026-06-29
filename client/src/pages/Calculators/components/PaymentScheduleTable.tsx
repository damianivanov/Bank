import { formatCurrency } from "@/lib/formatters";
import type { PaymentScheduleItem } from "@/types";

type PaymentScheduleTableProps = {
  schedule: PaymentScheduleItem[];
};

export default function PaymentScheduleTable({ schedule }: PaymentScheduleTableProps) {
  return (
    <div>
      <h3 className="mb-3 text-lg font-bold">Погасителен план</h3>
      <div className="max-h-[28rem] overflow-auto rounded-2xl border border-black/5 dark:border-white/10">
        <table className="w-full text-sm">
          <thead className="sticky top-0 bg-white text-tertiary dark:bg-[#131c2e]">
            <tr className="border-b border-black/5 dark:border-white/10">
              <th className="px-3 py-2 text-left font-semibold">№</th>
              <th className="px-3 py-2 text-right font-semibold">Остатък</th>
              <th className="px-3 py-2 text-right font-semibold">Вноска</th>
              <th className="px-3 py-2 text-right font-semibold">Главница</th>
              <th className="px-3 py-2 text-right font-semibold">Лихва</th>
              <th className="px-3 py-2 text-right font-semibold">Такси</th>
              <th className="px-3 py-2 text-right font-semibold">Паричен поток</th>
            </tr>
          </thead>
          <tbody>
            {schedule.map((row) => {
              const isOpening = row.month === 0;
              return (
                <tr key={row.month} className="border-b border-black/5 last:border-0 dark:border-white/5">
                  <td className="px-3 py-2 text-left text-secondary">{row.month}</td>
                  <td className="px-3 py-2 text-right">{formatCurrency(row.remainingBalance)}</td>
                  <td className="px-3 py-2 text-right">{isOpening ? "—" : formatCurrency(row.payment)}</td>
                  <td className="px-3 py-2 text-right">{isOpening ? "—" : formatCurrency(row.principal)}</td>
                  <td className="px-3 py-2 text-right">{isOpening ? "—" : formatCurrency(row.interest)}</td>
                  <td className="px-3 py-2 text-right">{row.fees > 0 ? formatCurrency(row.fees) : "—"}</td>
                  <td className={`px-3 py-2 text-right ${row.cashFlow >= 0 ? "text-emerald-500" : "text-rose-500"}`}>
                    {formatCurrency(row.cashFlow)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
