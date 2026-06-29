import { formatDateTime } from "@/lib/formatters";
import { EntityGrid } from "@/shared/components";
import type { ApiError } from "@/types";

type ErrorsGridProps = {
  errors: ApiError[];
  isLoading: boolean;
  onSelect: (error: ApiError) => void;
};

export default function ErrorsGrid({ errors, isLoading, onSelect }: ErrorsGridProps) {
  if (isLoading) {
    return <p className="mt-6 text-sm text-secondary">Зареждане на грешки...</p>;
  }

  return (
    <div className="mt-6">
      <EntityGrid>
        <thead>
          <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
            <th className="px-4 py-3">Дата</th>
            <th className="px-4 py-3">Съобщение</th>
            <th className="px-4 py-3">Път</th>
            <th className="px-4 py-3">Потребител</th>
          </tr>
        </thead>
        <tbody>
          {errors.map((error) => (
            <tr key={error.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="whitespace-nowrap px-4 py-3 text-secondary">{formatDateTime(error.dateCreated)}</td>
              <td className="px-4 py-3">
                <button
                  type="button"
                  onClick={() => onSelect(error)}
                  title={error.message}
                  className="block max-w-md truncate text-left font-medium text-foreground underline-offset-4 transition hover:underline"
                >
                  {error.message}
                </button>
              </td>
              <td
                className="max-w-xs truncate px-4 py-3 font-mono text-xs text-secondary"
                title={error.path ?? undefined}
              >
                {error.path ?? "-"}
              </td>
              <td className="whitespace-nowrap px-4 py-3">{error.userName ?? "-"}</td>
            </tr>
          ))}
          {errors.length === 0 ? (
            <tr>
              <td colSpan={4} className="px-4 py-6 text-center text-sm text-secondary">
                Няма намерени грешки.
              </td>
            </tr>
          ) : null}
        </tbody>
      </EntityGrid>
    </div>
  );
}
