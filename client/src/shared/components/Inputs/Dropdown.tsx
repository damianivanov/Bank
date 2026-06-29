import {
  Children,
  isValidElement,
  useCallback,
  useEffect,
  useId,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type ChangeEvent,
  type CSSProperties,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent as ReactMouseEvent,
  type ReactElement,
  type ReactNode,
  type SelectHTMLAttributes,
} from "react";
import { createPortal } from "react-dom";
import { LuCheck, LuChevronDown, LuSearch, LuX } from "react-icons/lu";
import FormField from "./FormField";

type DropdownValue = string | number;

export type DropdownOption<TValue extends DropdownValue = string> = {
  label: string;
  value: TValue;
  disabled?: boolean;
  keywords?: string[];
  imageUrl?: string;
};

type ParsedDropdownOption = DropdownOption<string>;

type DropdownProps = Omit<SelectHTMLAttributes<HTMLSelectElement>, "children" | "onChange" | "value" | "defaultValue"> & {
  label: string;
  hideLabel?: boolean;
  error?: string;
  value?: DropdownValue;
  defaultValue?: DropdownValue;
  onChange?: (event: ChangeEvent<HTMLSelectElement>) => void;
  children?: ReactNode;
  options?: readonly DropdownOption[];
  placeholder?: string;
  searchable?: boolean;
  searchPlaceholder?: string;
  onSearchChange?: (term: string) => void;
  loading?: boolean;
  emptyText?: string;
  menuClassName?: string;
  optionClassName?: string;
  clearable?: boolean;
  hideScrollbar?: boolean;
};

const VIEWPORT_PADDING_PX = 12;
const MENU_OFFSET_PX = 8;
const MENU_MAX_HEIGHT_PX = 320;
const MENU_MIN_HEIGHT_PX = 120;
const SEARCH_SECTION_HEIGHT_PX = 62;

function joinClassNames(...classNames: Array<string | undefined | false | null>): string {
  return classNames.filter(Boolean).join(" ");
}

function normalizeOptionValue(value: unknown): string {
  if (value === null || value === undefined) {
    return "";
  }

  return String(value);
}

function flattenNodeText(node: ReactNode): string {
  if (typeof node === "string" || typeof node === "number") {
    return String(node);
  }

  if (Array.isArray(node)) {
    return node.map((nestedNode) => flattenNodeText(nestedNode)).join("");
  }

  if (isValidElement<{ children?: ReactNode }>(node)) {
    return flattenNodeText(node.props.children);
  }

  return "";
}

function parseOptionsFromChildren(children: ReactNode): ParsedDropdownOption[] {
  return Children.toArray(children).flatMap((child) => {
    if (!isValidElement(child)) {
      return [];
    }

    if (typeof child.type !== "string" || child.type.toLowerCase() !== "option") {
      return [];
    }

    const optionElement = child as ReactElement<{
      value?: unknown;
      label?: string;
      disabled?: boolean;
      children?: ReactNode;
    }>;

    const optionValue = normalizeOptionValue(optionElement.props.value);
    const optionLabel = (optionElement.props.label ?? flattenNodeText(optionElement.props.children)).trim();

    return [
      {
        value: optionValue,
        label: optionLabel || optionValue,
        disabled: optionElement.props.disabled,
      },
    ];
  });
}

function findFirstEnabledIndex(options: readonly ParsedDropdownOption[]): number {
  return options.findIndex((option) => !option.disabled);
}

function findNextEnabledIndex(
  options: readonly ParsedDropdownOption[],
  startIndex: number,
  direction: 1 | -1,
): number {
  if (!options.length) {
    return -1;
  }

  let nextIndex = startIndex;
  for (let step = 0; step < options.length; step += 1) {
    nextIndex = (nextIndex + direction + options.length) % options.length;
    if (!options[nextIndex]?.disabled) {
      return nextIndex;
    }
  }

  return -1;
}

export default function Dropdown({
  label,
  hideLabel = false,
  error,
  className = "",
  children,
  options,
  placeholder = "Изберете...",
  searchable = false,
  searchPlaceholder = "Търсене...",
  onSearchChange,
  loading = false,
  emptyText = "Няма намерени опции",
  menuClassName = "",
  optionClassName = "",
  clearable = false,
  hideScrollbar = false,
  value,
  defaultValue,
  onChange,
  name,
  id,
  required = false,
  disabled = false,
  ...nativeSelectProps
}: DropdownProps) {
  const rootRef = useRef<HTMLDivElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const searchInputRef = useRef<HTMLInputElement | null>(null);
  const selectRef = useRef<HTMLSelectElement | null>(null);
  const listboxId = useId();
  const isControlled = value !== undefined;
  const [uncontrolledValue, setUncontrolledValue] = useState(() => normalizeOptionValue(defaultValue));
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [highlightedIndex, setHighlightedIndex] = useState(-1);
  const [menuPlacement, setMenuPlacement] = useState<"top" | "bottom">("bottom");
  const [menuMaxHeight, setMenuMaxHeight] = useState(MENU_MAX_HEIGHT_PX);
  // Менюто се рендира през портал към body, за да не се отрязва от контейнери с overflow
  // (напр. modal). Затова пазим координатите му спрямо viewport-а (fixed позициониране).
  const [menuPosition, setMenuPosition] = useState<{ left: number; width: number; top?: number; bottom?: number }>({
    left: 0,
    width: 0,
  });

  const selectedValue = isControlled ? normalizeOptionValue(value) : uncontrolledValue;
  // Сървърно търсене: родителят зарежда опциите според въведеното, а ние не филтрираме от клиента.
  const isServerSearch = onSearchChange !== undefined;
  const effectiveSearchable = searchable || isServerSearch;
  const [cachedSelectedOption, setCachedSelectedOption] = useState<ParsedDropdownOption | null>(null);

  const dropdownOptions = useMemo<ParsedDropdownOption[]>(() => {
    if (options !== undefined) {
      return options.map((option) => ({
        ...option,
        value: normalizeOptionValue(option.value),
      }));
    }

    return parseOptionsFromChildren(children);
  }, [children, options]);

  const selectedOption = useMemo(() => {
    const fromOptions = dropdownOptions.find((option) => option.value === selectedValue);
    if (fromOptions) {
      return fromOptions;
    }

    // При сървърно търсене избраната опция може вече да я няма в текущите резултати — пазим я
    // кеширана, за да остане видим етикетът ѝ след като списъкът се презареди.
    if (cachedSelectedOption && cachedSelectedOption.value === selectedValue) {
      return cachedSelectedOption;
    }

    return null;
  }, [dropdownOptions, selectedValue, cachedSelectedOption]);

  const nativeOptions = useMemo(() => {
    if (selectedOption && !dropdownOptions.some((option) => option.value === selectedOption.value)) {
      return [selectedOption, ...dropdownOptions];
    }

    return dropdownOptions;
  }, [dropdownOptions, selectedOption]);

  const filteredOptions = useMemo(() => {
    // При сървърно търсене опциите вече са филтрирани от родителя — не филтрираме повторно.
    if (isServerSearch) {
      return dropdownOptions;
    }

    const normalizedQuery = search.trim().toLowerCase();
    if (!normalizedQuery) {
      return dropdownOptions;
    }

    return dropdownOptions.filter((option) => {
      const searchHaystack = [option.label, option.value, ...(option.keywords ?? [])]
        .join(" ")
        .toLowerCase();

      return searchHaystack.includes(normalizedQuery);
    });
  }, [dropdownOptions, search, isServerSearch]);

  const hasSelectedOption = selectedOption !== null;
  const displayLabel = selectedOption?.label ?? placeholder;

  const closeMenu = useCallback(() => {
    setOpen(false);
    setSearch("");
  }, []);

  const updateMenuLayout = useCallback(() => {
    if (!rootRef.current) {
      return;
    }

    const rootRect = rootRef.current.getBoundingClientRect();
    const availableBelow = window.innerHeight - rootRect.bottom - VIEWPORT_PADDING_PX - MENU_OFFSET_PX;
    const availableAbove = rootRect.top - VIEWPORT_PADDING_PX - MENU_OFFSET_PX;
    const shouldOpenAbove = availableBelow < 220 && availableAbove > availableBelow;
    const availableSpace = shouldOpenAbove ? availableAbove : availableBelow;
    const resolvedMenuMaxHeight = Math.max(
      MENU_MIN_HEIGHT_PX,
      Math.min(MENU_MAX_HEIGHT_PX, availableSpace),
    );

    setMenuPlacement(shouldOpenAbove ? "top" : "bottom");
    setMenuMaxHeight(resolvedMenuMaxHeight);
    setMenuPosition({
      left: rootRect.left,
      width: rootRect.width,
      top: shouldOpenAbove ? undefined : rootRect.bottom + MENU_OFFSET_PX,
      bottom: shouldOpenAbove ? window.innerHeight - rootRect.top + MENU_OFFSET_PX : undefined,
    });
  }, []);

  useEffect(() => {
    if (!open) {
      return;
    }

    const selectedIndex = filteredOptions.findIndex((option) => option.value === selectedValue && !option.disabled);
    if (selectedIndex >= 0) {
      setHighlightedIndex(selectedIndex);
      return;
    }

    setHighlightedIndex(findFirstEnabledIndex(filteredOptions));
  }, [filteredOptions, open, selectedValue]);

  useEffect(() => {
    if (!open || !effectiveSearchable) {
      return;
    }

    searchInputRef.current?.focus();
  }, [open, effectiveSearchable]);

  useLayoutEffect(() => {
    if (!open) {
      return;
    }

    updateMenuLayout();

    const handleViewportChange = () => {
      updateMenuLayout();
    };

    window.addEventListener("resize", handleViewportChange);
    window.addEventListener("scroll", handleViewportChange, true);

    return () => {
      window.removeEventListener("resize", handleViewportChange);
      window.removeEventListener("scroll", handleViewportChange, true);
    };
  }, [open, updateMenuLayout]);

  useEffect(() => {
    const handleDocumentMouseDown = (event: MouseEvent) => {
      const target = event.target as Node;
      // Менюто живее в портал извън rootRef, затова проверяваме и него — иначе click върху
      // опция би се изтълкувал като "клик навън" и менюто би се затворило преди избора.
      if (!rootRef.current?.contains(target) && !menuRef.current?.contains(target)) {
        closeMenu();
      }
    };

    document.addEventListener("mousedown", handleDocumentMouseDown);
    return () => {
      document.removeEventListener("mousedown", handleDocumentMouseDown);
    };
  }, [closeMenu]);

  useEffect(() => {
    if (!open || !disabled) {
      return;
    }

    closeMenu();
  }, [closeMenu, disabled, open]);

  const emitChange = useCallback(
    (nextValue: string) => {
      if (!isControlled) {
        setUncontrolledValue(nextValue);
      }

      if (!onChange) {
        return;
      }

      if (selectRef.current) {
        selectRef.current.value = nextValue;
        const syntheticEvent = {
          target: selectRef.current,
          currentTarget: selectRef.current,
        } as ChangeEvent<HTMLSelectElement>;
        onChange(syntheticEvent);
        return;
      }

      const fallbackTarget = {
        name: name ?? "",
        value: nextValue,
      } as HTMLSelectElement;

      onChange({
        target: fallbackTarget,
        currentTarget: fallbackTarget,
      } as ChangeEvent<HTMLSelectElement>);
    },
    [isControlled, name, onChange],
  );

  const selectOption = useCallback(
    (option: ParsedDropdownOption) => {
      if (option.disabled) {
        return;
      }

      emitChange(option.value);
      setCachedSelectedOption(option);
      closeMenu();
    },
    [closeMenu, emitChange],
  );

  const moveHighlight = useCallback(
    (direction: 1 | -1) => {
      if (!filteredOptions.length) {
        return;
      }

      const baseIndex = highlightedIndex >= 0 ? highlightedIndex : 0;
      const nextIndex = findNextEnabledIndex(filteredOptions, baseIndex, direction);
      if (nextIndex >= 0) {
        setHighlightedIndex(nextIndex);
      }
    },
    [filteredOptions, highlightedIndex],
  );

  const handleRootKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLDivElement>) => {
      if (disabled) {
        return;
      }

      if (event.target instanceof HTMLInputElement) {
        if (event.key === "Escape") {
          event.preventDefault();
          closeMenu();
        }
        return;
      }

      if (event.key === "ArrowDown") {
        event.preventDefault();
        if (!open) {
          setOpen(true);
          return;
        }
        moveHighlight(1);
        return;
      }

      if (event.key === "ArrowUp") {
        event.preventDefault();
        if (!open) {
          setOpen(true);
          return;
        }
        moveHighlight(-1);
        return;
      }

      if (event.key === "Enter") {
        event.preventDefault();
        if (!open) {
          setOpen(true);
          return;
        }

        const highlightedOption = filteredOptions[highlightedIndex];
        if (highlightedOption) {
          selectOption(highlightedOption);
        }
        return;
      }

      if (event.key === " " && !open) {
        event.preventDefault();
        setOpen(true);
        return;
      }

      if (event.key === "Escape") {
        event.preventDefault();
        closeMenu();
      }
    },
    [closeMenu, disabled, filteredOptions, highlightedIndex, moveHighlight, open, selectOption],
  );

  const handleTriggerClick = useCallback(() => {
    if (disabled) {
      return;
    }

    setOpen((currentState) => !currentState);
  }, [disabled]);

  const handleOptionMouseEnter = useCallback((index: number) => {
    setHighlightedIndex(index);
  }, []);

  const handleSearchChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const nextValue = event.target.value;
      setSearch(nextValue);
      onSearchChange?.(nextValue);
    },
    [onSearchChange],
  );

  const handleNativeSelectChange = useCallback(
    (event: ChangeEvent<HTMLSelectElement>) => {
      if (!isControlled) {
        setUncontrolledValue(event.target.value);
      }

      onChange?.(event);
    },
    [isControlled, onChange],
  );

  const handleClearSelectionClick = useCallback(
    (event: ReactMouseEvent<HTMLButtonElement>) => {
      event.stopPropagation();
      emitChange("");
      closeMenu();
    },
    [closeMenu, emitChange],
  );

  const listMaxHeight = effectiveSearchable
    ? Math.max(80, menuMaxHeight - SEARCH_SECTION_HEIGHT_PX)
    : menuMaxHeight;
  const menuAnimationClassName =
    menuPlacement === "top" ? "bank-select-menu-enter-up origin-bottom" : "bank-select-menu-enter-down origin-top";
  const menuStyle: CSSProperties = {
    position: "fixed",
    left: menuPosition.left,
    width: menuPosition.width,
    maxHeight: menuMaxHeight,
    ...(menuPosition.top !== undefined ? { top: menuPosition.top } : { bottom: menuPosition.bottom }),
  };

  return (
    <FormField label={label} hideLabel={hideLabel} error={error}>
      <div
        ref={rootRef}
        className={joinClassNames("bank-select-root relative", className)}
        data-open={open ? "true" : undefined}
        onKeyDown={handleRootKeyDown}
      >
        <select
          {...nativeSelectProps}
          ref={selectRef}
          id={id}
          name={name}
          value={selectedValue}
          required={required}
          disabled={disabled}
          onChange={handleNativeSelectChange}
          className="bank-select-native"
          tabIndex={-1}
          aria-hidden="true"
        >
          {!required ? <option value="">{placeholder}</option> : null}
          {nativeOptions.map((option) => (
            <option key={option.value} value={option.value} disabled={option.disabled}>
              {option.label}
            </option>
          ))}
        </select>

        <div
          role="button"
          tabIndex={disabled ? -1 : 0}
          aria-disabled={disabled || undefined}
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={listboxId}
          onClick={handleTriggerClick}
          className={joinClassNames(
            "bank-select-trigger flex w-full items-center justify-between gap-2 px-4 py-3 text-left text-sm outline-none",
            disabled && "opacity-[0.65] cursor-not-allowed",
          )}
        >
          <span className={joinClassNames("truncate", hasSelectedOption ? "text-[var(--text-primary)]" : "text-[var(--input-placeholder)]")}>
            {displayLabel}
          </span>

          <span className="flex shrink-0 items-center gap-1">
            {clearable && hasSelectedOption && !disabled ? (
              <button
                type="button"
                onClick={handleClearSelectionClick}
                aria-label="Изчисти избора"
                className="bank-select-clear rounded-full p-0.5 transition-colors"
              >
                <LuX className="h-4 w-4" />
              </button>
            ) : null}

            <LuChevronDown className="bank-select-chevron h-5 w-5" />
          </span>
        </div>

        {open
          ? createPortal(
          <div
            ref={menuRef}
            className={joinClassNames(
              "bank-select-menu z-[60] overflow-hidden rounded-2xl p-1",
              menuAnimationClassName,
              menuClassName,
            )}
            style={menuStyle}
          >
            {effectiveSearchable ? (
              <div className="bank-select-search-wrap mb-1">
                <div className="bank-select-search flex h-10 items-center gap-2 px-3">
                  <LuSearch className="text-tertiary h-4 w-4 shrink-0" />
                  <input
                    ref={searchInputRef}
                    value={search}
                    onChange={handleSearchChange}
                    placeholder={searchPlaceholder}
                    className="bank-select-search-input w-full bg-transparent text-sm outline-none"
                  />
                </div>
              </div>
            ) : null}

            <ul
              id={listboxId}
              role="listbox"
              className={joinClassNames("bank-select-list", hideScrollbar ? "bank-scrollbar-hidden" : "bank-scrollbar")}
              style={{ maxHeight: listMaxHeight }}
            >
              {loading ? (
                <li className="text-tertiary px-4 py-2.5 text-sm">Зареждане...</li>
              ) : filteredOptions.length === 0 ? (
                <li className="text-tertiary px-4 py-2.5 text-sm">{emptyText}</li>
              ) : (
                filteredOptions.map((option, index) => {
                  const isSelected = option.value === selectedValue;
                  const isHighlighted = index === highlightedIndex;
                  const optionClassNames = joinClassNames(
                    "bank-select-option",
                    isSelected && "bank-select-option-selected",
                    !isSelected && isHighlighted && !option.disabled && "bank-select-option-highlighted",
                    option.disabled && "bank-select-option-disabled",
                    optionClassName,
                  );

                  return (
                    <li key={option.value} role="option" aria-selected={isSelected} className="bank-select-option-item">
                      <button
                        type="button"
                        disabled={option.disabled}
                        onMouseEnter={() => handleOptionMouseEnter(index)}
                        onClick={() => selectOption(option)}
                        className={optionClassNames}
                      >
                        <span className="bank-select-option-label">
                          {option.imageUrl ? (
                            <img
                              src={option.imageUrl}
                              alt=""
                              aria-hidden="true"
                              loading="lazy"
                              className="h-6 w-6 shrink-0 rounded-lg object-cover"
                            />
                          ) : null}
                          <span className="truncate">{option.label}</span>
                        </span>

                        {isSelected ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                      </button>
                    </li>
                  );
                })
              )}
            </ul>
          </div>,
              document.body,
            )
          : null}
      </div>
    </FormField>
  );
}
