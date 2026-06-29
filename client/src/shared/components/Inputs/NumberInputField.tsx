import type { ChangeEvent, InputHTMLAttributes, ReactNode } from "react";
import { cleanNumberInput, formatNumberInput } from "@/lib/numberInput";
import FormField from "./FormField";

export type NumberInputFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, "value" | "onChange" | "type"> & {
  label: string;
  error?: string;
  value: string;
  onValueChange: (raw: string) => void;
  /** Адорнмент, закотвен вдясно в полето (напр. "€" за парични суми). */
  suffix?: ReactNode;
};

const SIGNIFICANT = /[\d.]/;

export default function NumberInputField({
  label,
  error,
  value,
  onValueChange,
  suffix,
  className = "",
  ...props
}: NumberInputFieldProps) {
  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const input = event.target;
    const caret = input.selectionStart ?? input.value.length;
    const significantBeforeCaret = cleanNumberInput(input.value.slice(0, caret)).length;

    const raw = cleanNumberInput(input.value);
    const formatted = formatNumberInput(raw);

    // Връщаме каретката върху групирания низ, като броим колко цифри/десетични
    // символа стоят преди нея, така че добавените интервали да не я отместват.
    let position = 0;
    let seen = 0;
    while (position < formatted.length && seen < significantBeforeCaret) {
      if (SIGNIFICANT.test(formatted[position])) {
        seen += 1;
      }
      position += 1;
    }

    // Поправяме DOM-а синхронно, за да не остават изчистените символи, дори
    // когато raw стойността е същата и React пропуска повторния рендер.
    input.value = formatted;
    input.setSelectionRange(position, position);

    onValueChange(raw);
  };

  return (
    <FormField label={label} error={error}>
      <div className="relative">
        <input
          type="text"
          inputMode="decimal"
          autoComplete="off"
          value={formatNumberInput(value)}
          onChange={handleChange}
          className={`bank-input py-2.5 text-sm ${suffix ? "pl-3 pr-9" : "px-3"} ${error ? "bank-input-error" : ""} ${className}`.trim()}
          aria-invalid={error ? true : undefined}
          {...props}
        />
        {suffix ? (
          <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm font-semibold text-tertiary">
            {suffix}
          </span>
        ) : null}
      </div>
    </FormField>
  );
}
