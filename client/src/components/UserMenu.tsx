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
  const displayName = buildDisplayName(user.firstName, user.lastName) || "Bank User";
  const initials = buildInitials(user.firstName, user.lastName, user.email);
  const avatarToneClassName = getAvatarToneClassName(user.id);

  const containerClassName = `relative ${className}`.trim();

  const triggerClassName = isLightMode
    ? "flex w-full items-center gap-3 rounded-full border border-slate-300 bg-white/85 px-3 py-2.5 text-left shadow-sm transition hover:bg-white"
    : "flex w-full items-center gap-3 rounded-full border border-slate-600 bg-slate-900/85 px-3 py-2.5 text-left shadow-sm transition hover:bg-slate-900";

  const triggerTextClassName = isLightMode
    ? "min-w-0 flex-1 truncate text-sm font-semibold text-slate-900"
    : "min-w-0 flex-1 truncate text-sm font-semibold text-slate-100";

  const menuSurfaceClassName = isLightMode
    ? "absolute right-0 bottom-full left-0 z-30 mb-2 rounded-3xl border border-slate-300 bg-white/95 p-1.5 shadow-2xl backdrop-blur"
    : "absolute right-0 bottom-full left-0 z-30 mb-2 rounded-3xl border border-slate-700 bg-slate-950/95 p-1.5 shadow-2xl backdrop-blur";

  const menuItemClassName = isLightMode
    ? "flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-800 transition hover:bg-slate-100"
    : "flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-100 transition hover:bg-slate-800";

  const userNameClassName = isLightMode
    ? "truncate text-sm font-semibold text-slate-900"
    : "truncate text-sm font-semibold text-slate-100";

  const userEmailClassName = isLightMode
    ? "truncate text-xs text-slate-500"
    : "truncate text-xs text-slate-400";

  const dividerClassName = isLightMode ? "border-slate-200" : "border-slate-800";
  const iconClassName = isLightMode ? "h-4 w-4 text-slate-500" : "h-4 w-4 text-slate-400";

  const themeLabelClassName = isLightMode
    ? "text-sm font-medium text-slate-600"
    : "text-sm font-medium text-slate-300";

  const themeSwitchClassName = isLightMode
    ? "relative inline-flex h-6 w-11 items-center rounded-full border transition"
    : "relative inline-flex h-6 w-11 items-center rounded-full border transition";

  const themeSwitchKnobClassName = isLightMode
    ? "inline-block h-5 w-5 translate-x-5 rounded-full shadow transition"
    : "inline-block h-5 w-5 translate-x-0.5 rounded-full shadow transition";

  const moonIconClassName = isLightMode ? "h-4 w-4 text-emerald-600" : "h-4 w-4 text-emerald-300";
  const sunIconClassName = isLightMode ? "h-4 w-4 text-amber-500" : "h-4 w-4 text-amber-300";
  const dangerItemClassName = isLightMode
    ? "flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-left text-sm font-medium text-rose-600 transition hover:bg-rose-50"
    : "flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-left text-sm font-medium text-rose-300 transition hover:bg-rose-950/30";

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
        className={triggerClassName}
        aria-haspopup="menu"
        aria-expanded={isOpen}
      >
        <span className={`flex h-9 w-9 items-center justify-center rounded-full text-xs font-bold ${avatarToneClassName}`}>
          {initials}
        </span>
        <span className={triggerTextClassName}>{displayName}</span>
        <Menu className={iconClassName} />
      </button>

      {isOpen ? (
        <div role="menu" className={menuSurfaceClassName}>
          <div className={`border-b px-3 py-2 ${dividerClassName}`}>
            <div className="min-w-0">
              <p className={userNameClassName}>{displayName}</p>
              {user.email ? <p className={userEmailClassName}>{user.email}</p> : null}
            </div>
          </div>

          <Link to="/profile" onClick={handleProfileClick} className={`${menuItemClassName} mt-1`} role="menuitem">
            <UserRound className="h-4 w-4" />
            Profile
          </Link>

          {isAdminUser ? (
            <Link to="/management" onClick={handleAdminClick} className={menuItemClassName} role="menuitem">
              <Shield className="h-4 w-4" />
              Admin
            </Link>
          ) : null}

          <div className={`mt-1 border-t px-2 pt-1 ${dividerClassName}`}>
            <div className="flex items-center justify-between px-1 py-2.5">
              <span className={themeLabelClassName}>Theme</span>
              <div className="flex items-center gap-2.5">
                <Moon className={moonIconClassName} aria-hidden="true" />
                <button
                  type="button"
                  onClick={handleThemeToggle}
                  className={themeSwitchClassName}
                  style={
                    isLightMode
                      ? { borderColor: "var(--accent-border)", background: "var(--accent-soft)" }
                      : { borderColor: "var(--color-border-strong)", background: "rgba(15, 23, 42, 0.8)" }
                  }
                  role="menuitemcheckbox"
                  aria-checked={isLightMode}
                  aria-label={isLightMode ? "Switch to dark mode" : "Switch to light mode"}
                >
                  <span
                    className={themeSwitchKnobClassName}
                    style={isLightMode ? { background: "var(--accent)" } : { background: "#e2e8f0" }}
                  />
                </button>
                <Sun className={sunIconClassName} aria-hidden="true" />
              </div>
            </div>
          </div>

          <div className={`mt-1 border-t px-2 pt-1 ${dividerClassName}`}>
            <button type="button" onClick={handleLogoutClick} className={dangerItemClassName} role="menuitem">
              <LogOut className="h-4 w-4" />
              Logout
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
