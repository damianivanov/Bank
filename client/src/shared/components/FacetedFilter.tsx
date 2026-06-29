import { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { Check, ChevronDown, ListFilter } from "lucide-react";

export type FacetOption<V extends string = string> = {
  value: V;
  label: string;
  count?: number;
};

type FacetedFilterProps<V extends string = string> = {
  title: string;
  options: FacetOption<V>[];
  selected: V[];
  onToggle: (value: V) => void;
  onClear: () => void;
};

const MENU_VIEWPORT_PADDING_PX = 12;
const MENU_OFFSET_PX = 8;
const MENU_MIN_WIDTH_PX = 240;
const MENU_MIN_HEIGHT_PX = 120;
const MENU_MAX_HEIGHT_PX = 320;

type MenuPosition = {
  top: number;
  left: number;
  minWidth: number;
  maxHeight: number;
  placement: "top" | "bottom";
};

function cx(...classNames: Array<string | false | null | undefined>) {
  return classNames.filter(Boolean).join(" ");
}

// Многоизборен facet-филтър в popup: бутон-тригер с брояч на избраните + списък с опции (брой на всяка).
// Менюто остава отворено при избор (множествен избор) и се позиционира през портал като bank-select менютата.
export default function FacetedFilter<V extends string = string>({
  title,
  options,
  selected,
  onToggle,
  onClear,
}: FacetedFilterProps<V>) {
  const rootRef = useRef<HTMLDivElement | null>(null);
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState<MenuPosition | null>(null);

  const updateMenuPosition = useCallback(() => {
    if (!triggerRef.current) {
      return;
    }

    const rect = triggerRef.current.getBoundingClientRect();
    const availableBelow = window.innerHeight - rect.bottom - MENU_VIEWPORT_PADDING_PX - MENU_OFFSET_PX;
    const availableAbove = rect.top - MENU_VIEWPORT_PADDING_PX - MENU_OFFSET_PX;
    const shouldOpenAbove = availableBelow < 220 && availableAbove > availableBelow;
    const availableSpace = shouldOpenAbove ? availableAbove : availableBelow;
    const maxHeight = Math.max(MENU_MIN_HEIGHT_PX, Math.min(MENU_MAX_HEIGHT_PX, availableSpace));
    const top = shouldOpenAbove ? rect.top - MENU_OFFSET_PX : rect.bottom + MENU_OFFSET_PX;

    setMenuPosition({
      top,
      left: rect.left,
      minWidth: Math.max(MENU_MIN_WIDTH_PX, rect.width),
      maxHeight,
      placement: shouldOpenAbove ? "top" : "bottom",
    });
  }, []);

  useEffect(() => {
    if (!isOpen) {
      setMenuPosition(null);
      return;
    }

    updateMenuPosition();

    const handleViewportChange = () => updateMenuPosition();
    window.addEventListener("resize", handleViewportChange);
    window.addEventListener("scroll", handleViewportChange, true);

    return () => {
      window.removeEventListener("resize", handleViewportChange);
      window.removeEventListener("scroll", handleViewportChange, true);
    };
  }, [isOpen, updateMenuPosition]);

  useEffect(() => {
    const handleMouseDown = (event: MouseEvent) => {
      const inRoot = rootRef.current?.contains(event.target as Node) ?? false;
      const inMenu = menuRef.current?.contains(event.target as Node) ?? false;
      if (!inRoot && !inMenu) {
        setIsOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleMouseDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handleMouseDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, []);

  const selectedCount = selected.length;

  return (
    <div ref={rootRef} className="relative inline-flex">
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="bank-secondary-btn inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs font-semibold"
      >
        <ListFilter className="h-4 w-4 shrink-0" />
        <span>{title}</span>
        {selectedCount > 0 ? (
          <span className="bank-chip bank-chip-info rounded-full px-1.5 text-[0.65rem] font-bold leading-5">
            {selectedCount}
          </span>
        ) : null}
        <ChevronDown className={cx("h-4 w-4 shrink-0 transition-transform", isOpen && "rotate-180")} />
      </button>

      {isOpen && menuPosition
        ? createPortal(
            <div
              ref={menuRef}
              className={cx(
                "bank-select-menu fixed z-[70] overflow-hidden rounded-xl p-1",
                menuPosition.placement === "top"
                  ? "bank-select-menu-enter-up origin-bottom"
                  : "bank-select-menu-enter-down origin-top",
              )}
              style={{
                top: menuPosition.top,
                left: menuPosition.left,
                minWidth: menuPosition.minWidth,
                transform: menuPosition.placement === "top" ? "translateY(-100%)" : undefined,
              }}
            >
              <ul className="bank-select-list" style={{ maxHeight: menuPosition.maxHeight }}>
                {options.map((option) => {
                  const isSelected = selected.includes(option.value);
                  return (
                    <li key={option.value} className="bank-select-option-item">
                      <button
                        type="button"
                        onClick={() => onToggle(option.value)}
                        className={cx("bank-select-option", isSelected && "bank-select-option-selected")}
                      >
                        <span className="bank-select-option-label">{option.label}</span>
                        <span className="flex shrink-0 items-center gap-2">
                          {typeof option.count === "number" ? (
                            <span className="text-xs text-tertiary">{option.count}</span>
                          ) : null}
                          {isSelected ? <Check className="bank-select-check h-4 w-4 shrink-0" /> : null}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>

              {selectedCount > 0 ? (
                <div className="mt-1 border-t border-black/5 pt-1 dark:border-white/10">
                  <button
                    type="button"
                    onClick={onClear}
                    className="w-full rounded-lg px-3 py-2 text-center text-xs font-semibold text-tertiary transition hover:bg-black/5 dark:hover:bg-white/10"
                  >
                    Изчисти
                  </button>
                </div>
              ) : null}
            </div>,
            document.body,
          )
        : null}
    </div>
  );
}
