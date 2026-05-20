import type { SelectHTMLAttributes } from "react";
import FormField from "./FormField";

type DropdownProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
};

export default function Dropdown({ label, className = "", children, ...props }: DropdownProps) {
  return (
    <FormField label={label}>
      <select className={`bank-input px-3 py-2.5 text-sm ${className}`} {...props}>
        {children}
      </select>
    </FormField>
  );
}
