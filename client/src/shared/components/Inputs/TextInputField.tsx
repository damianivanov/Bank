import type { InputHTMLAttributes } from "react";
import FormField from "./FormField";

type TextInputFieldProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  error?: string;
};

export default function TextInputField({ label, error, className = "", ...props }: TextInputFieldProps) {
  return (
    <FormField label={label} error={error}>
      <input
        className={`bank-input px-3 py-2.5 text-sm ${error ? "bank-input-error" : ""} ${className}`.trim()}
        aria-invalid={error ? true : undefined}
        {...props}
      />
    </FormField>
  );
}
