import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { LogOut, Menu, Moon, Shield, Sun, UserRound } from "lucide-react";
import { isAdmin as hasAdminRole } from "@/lib/access";
import { useThemeStore } from "@/stores/themeStore";
import type { User } from "@/types";

type UserMenuProps = {
  user: User;
  onNavigate: () => void;
  onLogout: () => void;
  className?: string;
};

const avatarToneClassNames = [
  "bg-cyan-700 text-white",
  "bg-blue-700 text-white",
  "bg-indigo-700 text-white",
  "bg-violet-700 text-white",
  "bg-pink-700 text-white",
  "bg-red-700 text-white",
  "bg-orange-700 text-white",
  "bg-emerald-700 text-white",
  "bg-slate-700 text-white",
] as const;

function buildDisplayName(firstName?: string, lastName?: string): string {
  return [firstName?.trim(), lastName?.trim()].filter(Boolean).join(" ").trim();
}

function buildInitials(firstName?: string, lastName?: string, email?: string): string {
  const trimmedFirstName = firstName?.trim() ?? "";
  const trimmedLastName = lastName?.trim() ?? "";

  if (trimmedFirstName || trimmedLastName) {
    const firstInitial = trimmedFirstName.charAt(0);
    const secondInitial = trimmedLastName.charAt(0) || trimmedFirstName.charAt(1);
    const initials = `${firstInitial}${secondInitial}`.trim().toUpperCase();
    if (initials) {
      return initials;
    }
  }

  const emailPrefix = (email ?? "").split("@")[0].replace(/[^A-Za-z0-9]/g, "");
  return emailPrefix.slice(0, 2).toUpperCase() || "BU";
}

function getAvatarToneClassName(userId?: number): string {
  const normalizedUserId = Number.isFinite(userId) ? Math.abs(Math.trunc(userId ?? 0)) : 0;
  return avatarToneClassNames[normalizedUserId % avatarToneClassNames.length];
}

export default function UserMenu({
  user,
  onNavigate,
  onLogout,
  className = "",
}: UserMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const { theme, toggleTheme } = useThemeStore();

  const isAdminUser = hasAdminRole(user);
  const isLightMode = theme === "light";
  const displayName = buildDisplayName(user.firstName, user.lastName) || "Потребител";
  const initials = buildInitials(user.firstName, user.lastName, user.email);
  const avatarToneClassName = getAvatarToneClassName(user.id);
  const menuIconClassName = "h-4 w-4 text-slate-500 dark:text-slate-400";
  const sunToggleIconClassName = isLightMode ? "h-4 w-4 text-orange-600" : "h-4 w-4 text-orange-400/85";
  const moonToggleIconClassName = isLightMode ? "h-4 w-4 text-sky-400/85" : "h-4 w-4 text-sky-600";
  const containerClassName = `relative ${className}`.trim();

  const handleToggleMenu = () => {
    setIsOpen((current) => !current);
  };

  const handleCloseMenu = () => {
    setIsOpen(false);
  };

  const handleProfileClick = () => {
    onNavigate();
    handleCloseMenu();
  };

  const handleAdminClick = () => {
    onNavigate();
    handleCloseMenu();
  };

  const handleThemeToggle = () => {
    toggleTheme();
  };

  const handleLogoutClick = () => {
    onLogout();
    handleCloseMenu();
  };

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleDocumentMouseDown = (event: MouseEvent) => {
      const eventTarget = event.target as Node;
      if (containerRef.current?.contains(eventTarget)) {
        return;
      }

      handleCloseMenu();
    };

    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        handleCloseMenu();
      }
    };

    document.addEventListener("mousedown", handleDocumentMouseDown);
    document.addEventListener("keydown", handleDocumentKeyDown);

    return () => {
      document.removeEventListener("mousedown", handleDocumentMouseDown);
      document.removeEventListener("keydown", handleDocumentKeyDown);
    };
  }, [isOpen]);

  return (
    <div ref={containerRef} className={containerClassName}>
      <button
        type="button"
        onClick={handleToggleMenu}
        className="liquid-pill flex w-full items-center gap-3 rounded-full px-3 py-2.5 text-left"
        aria-haspopup="menu"
        aria-expanded={isOpen}
      >
        <span className={`flex h-9 w-9 items-center justify-center rounded-full text-xs font-bold ${avatarToneClassName}`}>
          {initials}
        </span>
        <span className="min-w-0 flex-1 truncate text-sm font-semibold text-slate-800 dark:text-slate-100">{displayName}</span>
        <Menu className={menuIconClassName} />
      </button>

      {isOpen ? (
        <div role="menu" className="liquid-user-menu absolute right-0 bottom-full left-0 z-30 mb-2 rounded-2xl p-1.5">
          <div className="liquid-divider border-b px-3 py-2">
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">{displayName}</p>
              {user.email ? <p className="truncate text-xs text-tertiary">{user.email}</p> : null}
            </div>
          </div>

          <Link
            to="/profile"
            onClick={handleProfileClick}
            className="liquid-nav-item mt-1 flex items-center gap-2 rounded-full px-3 py-2.5 text-sm font-medium"
            role="menuitem"
          >
            <UserRound className="h-4 w-4" />
            Профил
          </Link>

          {isAdminUser ? (
            <Link
              to="/management"
              onClick={handleAdminClick}
              className="liquid-nav-item flex items-center gap-2 rounded-full px-3 py-2.5 text-sm font-medium"
              role="menuitem"
            >
              <Shield className="h-4 w-4" />
              Администрация
            </Link>
          ) : null}

          <div className="liquid-divider mt-1 border-t px-1 pt-1">
            <div className="flex items-center justify-between px-3 py-2.5 text-sm font-medium">
              <span className="text-secondary">Тема</span>
              <div className="flex items-center gap-2.5">
                <Moon className={moonToggleIconClassName} aria-hidden="true" />
                <button
                  type="button"
                  onClick={handleThemeToggle}
                  className={`liquid-theme-switch ${isLightMode ? "liquid-theme-switch-active" : ""}`}
                  role="menuitemcheckbox"
                  aria-checked={isLightMode}
                  aria-label={isLightMode ? "Превключи към тъмна тема" : "Превключи към светла тема"}
                >
                  <span className="liquid-theme-switch-knob" />
                </button>
                <Sun className={sunToggleIconClassName} aria-hidden="true" />
              </div>
            </div>
          </div>

          <div className="liquid-divider mt-1 border-t px-1 pt-1">
            <button
              type="button"
              onClick={handleLogoutClick}
              className="liquid-nav-item liquid-pill-danger flex w-full items-center gap-2 rounded-full px-3 py-2.5 text-left text-sm font-medium"
              role="menuitem"
            >
              <LogOut className="h-4 w-4" />
              Изход
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
