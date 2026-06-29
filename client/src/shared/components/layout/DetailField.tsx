import type { ReactNode } from "react";

type DetailFieldProps = {
  label: string;
  children: ReactNode;
  valueClassName?: string;
  className?: string;
};

/**
 * Етикет + стойност — основната градивна единица на страниците с детайли.
 * Държи типографията на метаданните консистентна между сметки, кредити и клиенти.
 */
export function DetailField({ label, children, valueClassName, className }: DetailFieldProps) {
  return (
    <div className={className}>
      <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">{label}</p>
      <div className={`mt-1.5 text-sm font-medium tabular-nums ${valueClassName ?? ""}`.trim()}>{children}</div>
    </div>
  );
}
