import type { InputHTMLAttributes } from "react";
import FormField from "./FormField";

type TextInputFieldProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
};

export default function TextInputField({ label, className = "", ...props }: TextInputFieldProps) {
  return (
    <FormField label={label}>
      <input className={`bank-input px-3 py-2.5 text-sm ${className}`} {...props} />
    </FormField>
  );
}
