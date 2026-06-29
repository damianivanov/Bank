// `useGrouping: "always"` е валидно по време на изпълнение, но
// вграденият TS lib още типизира useGrouping като boolean, затова го разширяваме локално.
type GroupedNumberFormatOptions = Omit<Intl.NumberFormatOptions, "useGrouping"> & {
  useGrouping?: "always" | "auto" | "min2" | boolean;
};

export function formatCurrency(value: number): string {
  const options: GroupedNumberFormatOptions = {
    style: "currency",
    currency: "EUR",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    useGrouping: "always",
  };
  return new Intl.NumberFormat("bg-BG", options as Intl.NumberFormatOptions).format(value);
}

export function formatPercent(value: number): string {
  return `${value.toFixed(2)}%`;
}

export function formatIban(value: string | undefined): string {
  if (!value) {
    return "-";
  }
  // Групиране по 4 знака за по-лесно четене (ISO 13616 формат за печат).
  return value
    .replace(/\s+/g, "")
    .toUpperCase()
    .replace(/(.{4})/g, "$1 ")
    .trim();
}

export function formatDate(value: string | undefined): string {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return new Intl.DateTimeFormat("bg-BG", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date);
}

export function formatDateTime(value: string | undefined): string {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return new Intl.DateTimeFormat("bg-BG", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}
