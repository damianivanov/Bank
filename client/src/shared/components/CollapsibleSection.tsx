import { useState, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";

type CollapsibleSectionProps = {
  title: string;
  description?: string;
  defaultOpen?: boolean;
  /** Действие в заглавието (напр. бутон към прозорец). Стои извън toggle бутона, за да е самостоятелно кликаемо. */
  headerAction?: ReactNode;
  children: ReactNode;
};

export default function CollapsibleSection({
  title,
  description,
  defaultOpen = false,
  headerAction,
  children,
}: CollapsibleSectionProps) {
  const [open, setOpen] = useState(defaultOpen);
  const toggle = () => setOpen((value) => !value);

  return (
    <div className="bank-panel overflow-hidden rounded-2xl">
      {/* Два toggle-а (заглавието и стрелката) ограждат headerAction, който си остава отделен бутон. */}
      <div className="flex items-center gap-2 pr-3">
        <button
          type="button"
          onClick={toggle}
          aria-expanded={open}
          className="flex min-w-0 flex-1 items-center px-5 py-4 text-left"
        >
          <span className="min-w-0">
            <span className="block text-sm font-semibold">{title}</span>
            {description ? <span className="mt-0.5 block text-xs text-tertiary">{description}</span> : null}
          </span>
        </button>
        {headerAction ? <div className="shrink-0">{headerAction}</div> : null}
        <button
          type="button"
          onClick={toggle}
          aria-expanded={open}
          aria-label={open ? "Свий" : "Разгъни"}
          className="shrink-0 rounded-lg p-1.5 text-tertiary transition-colors hover:text-[var(--text-primary)]"
        >
          <ChevronDown className={`h-4 w-4 transition-transform duration-200 ease-out ${open ? "rotate-180" : ""}`} />
        </button>
      </div>
      {open ? (
        <div className="bank-collapse-in border-t border-black/5 px-5 py-4 dark:border-white/10">{children}</div>
      ) : null}
    </div>
  );
}
