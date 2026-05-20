import type { ReactNode } from "react";

type FormFieldProps = {
  label: string;
  children: ReactNode;
};

export default function FormField({ label, children }: FormFieldProps) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-tertiary">
        {label}
      </span>
      {children}
    </label>
  );
}
