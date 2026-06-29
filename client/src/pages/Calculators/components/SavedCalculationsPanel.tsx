import { Bookmark, Download, Trash2 } from "lucide-react";
import { formatDate } from "@/lib/formatters";
import { CalculatorType, type SavedCalculationModel } from "@/types";

const typeLabels: Record<CalculatorType, string> = {
  [CalculatorType.Credit]: "Кредит",
  [CalculatorType.Leasing]: "Лизинг",
  [CalculatorType.Refinancing]: "Рефинансиране",
};

type SavedCalculationsPanelProps = {
  items: SavedCalculationModel[];
  isLoading: boolean;
  loadingId: number | null;
  deletingId: number | null;
  onLoad: (id: number) => void;
  onDelete: (id: number) => void;
};

export default function SavedCalculationsPanel({
  items,
  isLoading,
  loadingId,
  deletingId,
  onLoad,
  onDelete,
}: SavedCalculationsPanelProps) {
  return (
    <div className="bank-panel rounded-2xl p-5">
      <div className="mb-4 flex items-center gap-2">
        <Bookmark className="h-5 w-5 text-tertiary" />
        <h2 className="text-lg font-bold">Запазени изчисления</h2>
      </div>

      {isLoading ? (
        <p className="text-sm text-secondary">Зареждане на запазените изчисления…</p>
      ) : items.length === 0 ? (
        <p className="text-sm text-secondary">
          Все още няма запазени изчисления. Направете изчисление и натиснете „Запази изчислението“, за да го запазите тук.
        </p>
      ) : (
        <ul className="space-y-2">
          {items.map((item) => {
            const busyLoad = loadingId === item.id;
            const busyDelete = deletingId === item.id;
            return (
              <li
                key={item.id}
                className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-black/5 px-4 py-3 dark:border-white/10"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold">{item.name}</p>
                  <p className="mt-0.5 text-xs text-tertiary">
                    {typeLabels[item.type]} · {formatDate(item.createdAtUtc)}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <button
                    type="button"
                    onClick={() => onLoad(item.id)}
                    disabled={busyLoad || busyDelete}
                    className="bank-secondary-btn bank-btn-action disabled:opacity-60"
                  >
                    <Download className="h-3.5 w-3.5" />
                    {busyLoad ? "Зареждане…" : "Зареди"}
                  </button>
                  <button
                    type="button"
                    onClick={() => onDelete(item.id)}
                    disabled={busyLoad || busyDelete}
                    className="bank-danger-btn bank-btn-action disabled:opacity-60"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                    {busyDelete ? "Изтриване…" : "Изтрий"}
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
