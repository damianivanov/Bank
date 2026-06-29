import { useCallback, useEffect, useRef, useState } from "react";
import { LuCheck, LuChevronDown } from "react-icons/lu";
import { createPortal } from "react-dom";
import { accessOptionLabels, type AccessOptionKey, type UserAccessPatch } from "../utils/userAccess.utils";

const ACCESS_MENU_VIEWPORT_PADDING_PX = 12;
const ACCESS_MENU_OFFSET_PX = 8;
const ACCESS_MENU_MIN_HEIGHT_PX = 120;
const ACCESS_MENU_MAX_HEIGHT_PX = 280;

type AccessDropdownProps = {
  disabled: boolean;
  values: UserAccessPatch;
  onToggleOption: (key: AccessOptionKey, nextValue: boolean) => void;
};

type AccessMenuPosition = {
  top: number;
  left: number;
  width: number;
  maxHeight: number;
  placement: "top" | "bottom";
};

function joinClassNames(...classNames: Array<string | false | null | undefined>) {
  return classNames.filter(Boolean).join(" ");
}

export default function AccessDropdown({ disabled, values, onToggleOption }: AccessDropdownProps) {
  const rootRef = useRef<HTMLDivElement | null>(null);
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState<AccessMenuPosition | null>(null);

  useEffect(() => {
    if (disabled && isOpen) {
      setIsOpen(false);
    }
  }, [disabled, isOpen]);

  const updateMenuPosition = useCallback(() => {
    if (!triggerRef.current) {
      return;
    }

    const triggerRect = triggerRef.current.getBoundingClientRect();
    const availableBelow = window.innerHeight - triggerRect.bottom - ACCESS_MENU_VIEWPORT_PADDING_PX - ACCESS_MENU_OFFSET_PX;
    const availableAbove = triggerRect.top - ACCESS_MENU_VIEWPORT_PADDING_PX - ACCESS_MENU_OFFSET_PX;
    const shouldOpenAbove = availableBelow < 180 && availableAbove > availableBelow;
    const availableSpace = shouldOpenAbove ? availableAbove : availableBelow;
    const maxHeight = Math.max(
      ACCESS_MENU_MIN_HEIGHT_PX,
      Math.min(ACCESS_MENU_MAX_HEIGHT_PX, availableSpace),
    );

    const top = shouldOpenAbove
      ? triggerRect.top - ACCESS_MENU_OFFSET_PX
      : triggerRect.bottom + ACCESS_MENU_OFFSET_PX;

    setMenuPosition({
      top,
      left: triggerRect.left,
      width: triggerRect.width,
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

    const handleViewportChange = () => {
      updateMenuPosition();
    };

    window.addEventListener("resize", handleViewportChange);
    window.addEventListener("scroll", handleViewportChange, true);

    return () => {
      window.removeEventListener("resize", handleViewportChange);
      window.removeEventListener("scroll", handleViewportChange, true);
    };
  }, [isOpen, updateMenuPosition]);

  useEffect(() => {
    const handleDocumentMouseDown = (event: MouseEvent) => {
      const isClickInsideRoot = rootRef.current?.contains(event.target as Node) ?? false;
      const isClickInsideMenu = menuRef.current?.contains(event.target as Node) ?? false;

      if (!isClickInsideRoot && !isClickInsideMenu) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleDocumentMouseDown);
    return () => {
      document.removeEventListener("mousedown", handleDocumentMouseDown);
    };
  }, []);

  const handleTriggerClick = () => {
    if (disabled) {
      return;
    }

    setIsOpen((currentValue) => !currentValue);
  };

  const handleActiveOptionClick = () => {
    onToggleOption("isActive", !values.isActive);
    setIsOpen(false);
  };

  const handleStaffOptionClick = () => {
    onToggleOption("isStaff", !values.isStaff);
    setIsOpen(false);
  };

  const handleAdminOptionClick = () => {
    onToggleOption("isAdmin", !values.isAdmin);
    setIsOpen(false);
  };

  const selectedLabels = (Object.keys(accessOptionLabels) as AccessOptionKey[])
    .filter((key) => values[key])
    .map((key) => accessOptionLabels[key]);

  const triggerLabel = selectedLabels.length > 0 ? selectedLabels.join(", ") : "Няма";

  return (
    <div ref={rootRef} className="relative">
      <button
        ref={triggerRef}
        type="button"
        disabled={disabled}
        onClick={handleTriggerClick}
        className={joinClassNames(
          "bank-select-trigger flex w-full items-center justify-between gap-2 rounded-full! px-3 py-2 text-xs font-semibold",
          disabled && "opacity-[0.65] cursor-not-allowed",
        )}
      >
        <span className="truncate">{triggerLabel}</span>
        <LuChevronDown className={joinClassNames("bank-select-chevron h-4 w-4", isOpen && "rotate-180")} />
      </button>

      {isOpen && menuPosition
        ? createPortal(
            <div
              ref={menuRef}
              className={joinClassNames(
                "bank-select-menu bank-access-select-menu fixed z-[70] overflow-hidden rounded-xl p-1",
                menuPosition.placement === "top" ? "bank-select-menu-enter-up origin-bottom" : "bank-select-menu-enter-down origin-top",
              )}
              style={{
                top: menuPosition.top,
                left: menuPosition.left,
                width: menuPosition.width,
                transform: menuPosition.placement === "top" ? "translateY(-100%)" : undefined,
              }}
            >
              <ul className="bank-select-list" style={{ maxHeight: menuPosition.maxHeight }}>
                <li className="bank-select-option-item">
                  <button
                    type="button"
                    onClick={handleActiveOptionClick}
                    className={joinClassNames("bank-select-option", values.isActive && "bank-select-option-selected")}
                  >
                    <span className="bank-select-option-label">{accessOptionLabels.isActive}</span>
                    {values.isActive ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                  </button>
                </li>
                <li className="bank-select-option-item">
                  <button
                    type="button"
                    onClick={handleStaffOptionClick}
                    className={joinClassNames("bank-select-option", values.isStaff && "bank-select-option-selected")}
                  >
                    <span className="bank-select-option-label">{accessOptionLabels.isStaff}</span>
                    {values.isStaff ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                  </button>
                </li>
                <li className="bank-select-option-item">
                  <button
                    type="button"
                    onClick={handleAdminOptionClick}
                    className={joinClassNames("bank-select-option", values.isAdmin && "bank-select-option-selected")}
                  >
                    <span className="bank-select-option-label">{accessOptionLabels.isAdmin}</span>
                    {values.isAdmin ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                  </button>
                </li>
              </ul>
            </div>,
            document.body,
          )
        : null}
    </div>
  );
}
