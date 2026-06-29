export type FieldErrors<TKeys extends string = string> = Partial<Record<TKeys, string>>;

/** Повтаря server/Bank.Core/Validation/CalculatorLimits.cs. */
export const calculatorLimits = {
  minTermMonths: 1,
  maxTermMonths: 360,
  minAmount: 0.01,
  maxAmount: 1_000_000_000,
  minRate: 0,
  maxRate: 1000,
  minPercent: 0,
  maxPercent: 100,
} as const;

/** Повтаря Identity password options (Program.cs) + RegisterRequest.MinLength(8). */
export const passwordMinLength = 8;

export function requiredText(value: string | null | undefined, label: string): string | undefined {
  return value && value.trim().length > 0 ? undefined : `Полето „${label}“ е задължително.`;
}

export function maxLengthText(value: string | null | undefined, max: number, label: string): string | undefined {
  return (value?.length ?? 0) <= max ? undefined : `Полето „${label}“ трябва да е най-много ${max} символа.`;
}

export function minLengthText(value: string | null | undefined, min: number, label: string): string | undefined {
  if (!value || value.length === 0) {
    return `Полето „${label}“ е задължително.`;
  }
  return value.length >= min ? undefined : `Полето „${label}“ трябва да е поне ${min} символа.`;
}

export function emailText(value: string | null | undefined, label = "Имейл"): string | undefined {
  if (!value || value.trim().length === 0) {
    return `Полето „${label}“ е задължително.`;
  }
  // Проверка на формата, която повтаря [EmailAddress].
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()) ? undefined : "Въведете валиден имейл адрес.";
}

/** Проверява стойност от формата като задължително число в интервала [min, max]. */
export function requiredNumberInRange(
  raw: string | number | null | undefined,
  min: number,
  max: number,
  label: string,
): string | undefined {
  if (raw === "" || raw === null || raw === undefined) {
    return `Полето „${label}“ е задължително.`;
  }
  const value = Number(raw);
  if (!Number.isFinite(value)) {
    return `Въведете валидна стойност за „${label}“.`;
  }
  if (value < min || value > max) {
    return `Стойността на „${label}“ трябва да е между ${min} и ${max}.`;
  }
  return undefined;
}

/** Същото като requiredNumberInRange, но празна стойност се приема (незадължително поле). */
export function optionalNumberInRange(
  raw: string | number | null | undefined,
  min: number,
  max: number,
  label: string,
): string | undefined {
  if (raw === "" || raw === null || raw === undefined) {
    return undefined;
  }
  return requiredNumberInRange(raw, min, max, label);
}

export function requiredIntegerInRange(
  raw: string | number | null | undefined,
  min: number,
  max: number,
  label: string,
): string | undefined {
  const rangeError = requiredNumberInRange(raw, min, max, label);
  if (rangeError) {
    return rangeError;
  }
  return Number.isInteger(Number(raw)) ? undefined : `Полето „${label}“ трябва да е цяло число.`;
}

export function optionalIntegerInRange(
  raw: string | number | null | undefined,
  min: number,
  max: number,
  label: string,
): string | undefined {
  if (raw === "" || raw === null || raw === undefined) {
    return undefined;
  }
  return requiredIntegerInRange(raw, min, max, label);
}

export function hasErrors(errors: FieldErrors): boolean {
  return Object.values(errors).some((message) => message !== undefined && message !== "");
}
