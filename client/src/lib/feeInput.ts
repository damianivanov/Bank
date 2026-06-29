import { FeeType } from "@/types";
import type { Fee } from "@/types";

export type FeeInput = {
  type: FeeType;
  value: string;
};

export function toFee(fee: FeeInput | undefined): Fee | undefined {
  if (!fee) {
    return undefined;
  }

  const trimmed = fee.value.trim();
  if (trimmed === "") {
    return undefined;
  }

  const value = Number.parseFloat(trimmed);
  if (!Number.isFinite(value)) {
    return undefined;
  }

  return { type: fee.type, value };
}
