import { useState } from "react";
import { Check, Copy } from "lucide-react";
import { toast } from "sonner";

type CopyableValueProps = {
  value: string;
  /** Текст за показване, ако се различава от копираното (напр. форматиран IBAN). */
  display?: string;
  /** Етикетът, използван за aria и съобщението при копиране (напр. "IBAN", "ЕГН"). */
  label?: string;
  /** Класове за самата стойност (размер на текста и т.н.). */
  className?: string;
};

/**
 * Моноширинна стойност с копче за копиране (IBAN, ЕГН, ЕИК...). Иконата се сменя
 * на отметка при успех и се връща след кратко. Копирането е честа операция в
 * банков контекст, затова е функционална микро-интеракция, не декорация.
 */
export function CopyableValue({ value, display, label, className }: CopyableValueProps) {
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      toast.success(label ? `${label} е копиран` : "Копирано");
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      toast.error("Копирането не бе успешно");
    }
  };

  return (
    <button
      type="button"
      onClick={copy}
      aria-label={label ? `Копирай ${label}` : "Копирай"}
      className="group inline-flex max-w-full items-center gap-2 text-left"
    >
      <span className={`truncate font-mono font-semibold tracking-tight ${className ?? ""}`.trim()}>{display ?? value}</span>
      <span className="shrink-0 text-tertiary transition-colors group-hover:text-accent">
        {copied ? <Check className="h-4 w-4 text-accent" /> : <Copy className="h-4 w-4" />}
      </span>
    </button>
  );
}
