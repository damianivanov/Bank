import type { ReactNode } from "react";

type FormFieldProps = {
  label: string;
  error?: string;
  hideLabel?: boolean;
  children: ReactNode;
};

export default function FormField({ label, error, hideLabel = false, children }: FormFieldProps) {
  return (
    <label className="bank-field block">
      <span
        className={`bank-field-label mb-1.5 block text-xs font-semibold uppercase tracking-wide text-tertiary${
          hideLabel ? " sr-only" : ""
        }`}
      >
        {label}
      </span>
      {children}
      {error ? (
        <span role="alert" className="bank-field-error mt-1.5 block text-xs font-medium text-rose-500">
          {error}
        </span>
      ) : null}
    </label>
  );
}
