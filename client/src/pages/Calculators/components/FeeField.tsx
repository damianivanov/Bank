import { Dropdown, NumberInputField } from "@/shared/components";
import { FeeType } from "@/types";
import type { FeeInput } from "@/lib/feeInput";

type FeeFieldProps = {
  label: string;
  fee: FeeInput | undefined;
  onChange: (fee: FeeInput | undefined) => void;
};

export default function FeeField({ label, fee, onChange }: FeeFieldProps) {
  const type = fee?.type ?? FeeType.Currency;
  const value = fee?.value ?? "";

  return (
    <div className="grid grid-cols-[1fr_7.5rem] gap-2">
      <NumberInputField
        label={label}
        value={value}
        onValueChange={(raw) => onChange(raw.trim() === "" ? undefined : { type, value: raw })}
      />
      <Dropdown
        label="Единица"
        value={type}
        onChange={(event) => onChange({ type: Number(event.target.value) as FeeType, value })}
      >
        <option value={FeeType.Currency}>EUR</option>
        <option value={FeeType.Percent}>%</option>
      </Dropdown>
    </div>
  );
}
