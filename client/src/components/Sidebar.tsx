import { useEffect, useState } from "react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { Menu, X } from "lucide-react";
import logoImage from "@/assets/logo.png";
import { hasAnyRole } from "@/lib/access";
import { useUserStore } from "@/stores/userStore";
import type { User, UserRole } from "@/types";
import MobileBottomNav from "./MobileBottomNav";
import { navSections } from "./navigation";
import UserMenu from "./UserMenu";

function getPrimaryItemClassName(isActive: boolean): string {
  const baseClassName =
    "bank-nav-item flex w-full items-center gap-3 rounded-xl border border-transparent px-4 py-3 text-left text-sm font-medium transition";

  return isActive ? `${baseClassName} bank-nav-item-active font-semibold` : baseClassName;
}

function AppLogo() {
  return (
    <Link to="/" className="flex items-center gap-3">
      <span className="bank-logo-badge flex h-12 w-12 items-center justify-center rounded-xl">
        <img src={logoImage} alt="" className="h-10 w-10 object-contain" />
      </span>
      <span>
        <span className="block text-xl font-bold tracking-tight">BankOps</span>
        <span className="block text-xs font-medium text-tertiary">Bank operations</span>
      </span>
    </Link>
  );
}

type PrimaryNavItemsProps = {
  user: User;
  onNavigate: () => void;
};

function canAccessItem(user: User, allowedRoles?: readonly UserRole[]) {
  if (!allowedRoles || allowedRoles.length === 0) {
    return true;
  }

  return hasAnyRole(user, allowedRoles);
}

function PrimaryNavItems({ user, onNavigate }: PrimaryNavItemsProps) {
  const visibleSections = navSections
    .map((section) => ({
      section: section.section,
      items: section.items.filter((item) => canAccessItem(user, item.allowedRoles)),
    }))
    .filter((section) => section.items.length > 0);

  return (
    <div className="space-y-5 pt-8">
      {visibleSections.map((section) => (
        <section key={section.section}>
          <p className="bank-section-title px-2 text-xs font-bold uppercase tracking-widest">
            {section.section}
          </p>
          <div className="mt-2 space-y-1.5">
            {section.items.map((item) => {
              const Icon = item.icon;

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  onClick={onNavigate}
                  className={({ isActive }) => getPrimaryItemClassName(isActive)}
                >
                  <Icon className="h-4 w-4" />
                  <span>{item.label}</span>
                </NavLink>
              );
            })}
          </div>
        </section>
      ))}
    </div>
  );
}

function AuthenticatedNav() {
  const { user, logout } = useUserStore();
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  const handleOpenMobile = () => {
    setIsMobileOpen(true);
  };

  const handleCloseMobile = () => {
    setIsMobileOpen(false);
  };

  const handleNavigate = () => {
    setIsMobileOpen(false);
  };

  const handleLogout = () => {
    void logout();
    setIsMobileOpen(false);
  };

  useEffect(() => {
    if (!isMobileOpen) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [isMobileOpen]);

  return (
    <>
      <aside className="hidden h-dvh w-72 shrink-0 md:block">
        <div className="bank-sidebar-panel flex h-full flex-col px-4 py-6">
          <AppLogo />
          <PrimaryNavItems user={user} onNavigate={handleNavigate} />
          <div className="mt-auto">
            <UserMenu user={user} onNavigate={handleNavigate} onLogout={handleLogout} />
          </div>
        </div>
      </aside>

      <header className="bank-surface sticky top-0 z-30 px-4 py-3 md:hidden">
        <div className="mx-auto flex max-w-6xl items-center justify-between">
          <AppLogo />
          <button
            type="button"
            onClick={handleOpenMobile}
            className="bank-secondary-btn inline-flex h-10 w-10 items-center justify-center rounded-full"
            aria-label="Open navigation"
            aria-expanded={isMobileOpen}
          >
            <Menu className="h-5 w-5" />
          </button>
        </div>
      </header>

      {isMobileOpen ? (
        <div className="fixed inset-0 z-50 md:hidden">
          <button
            type="button"
            className="bank-overlay absolute inset-0"
            onClick={handleCloseMobile}
            aria-label="Close navigation overlay"
          />
          <aside className="bank-sidebar-panel relative h-full w-5/6 max-w-sm py-3 px-4">
            <div className="flex h-full flex-col">
              <div className="flex items-center justify-between">
                <AppLogo />
                <button
                  type="button"
                  onClick={handleCloseMobile}
                  className="bank-secondary-btn inline-flex h-10 w-10 items-center justify-center rounded-full"
                  aria-label="Close navigation"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>
              <div className="min-h-0 flex-1 overflow-y-auto">
                <PrimaryNavItems user={user} onNavigate={handleNavigate} />
              </div>
              <UserMenu user={user} onNavigate={handleNavigate} onLogout={handleLogout} />
            </div>
          </aside>
        </div>
      ) : null}

      <MobileBottomNav user={user} onNavigate={handleNavigate} />
    </>
  );
}

function PublicNav() {
  const location = useLocation();
  const isAuthRoute = location.pathname === "/login" || location.pathname === "/register";

  return (
    <nav className="px-4 py-4">
      <div className="bank-surface mx-auto flex max-w-6xl items-center justify-between rounded-2xl px-4 py-3">
        <AppLogo />
        {isAuthRoute ? null : (
          <Link to="/login" className="bank-primary-btn rounded-xl px-4 py-2 text-sm font-semibold">
            Login
          </Link>
        )}
      </div>
    </nav>
  );
}

export default function Sidebar() {
  const { userLoaded, isAuthenticated } = useUserStore();

  if (!userLoaded) {
    return (
      <nav className="px-4 py-4">
        <div className="bank-surface mx-auto flex max-w-6xl items-center justify-between rounded-2xl px-4 py-3">
          <AppLogo />
          <span className="text-sm font-medium text-tertiary">Loading...</span>
        </div>
      </nav>
    );
  }

  return isAuthenticated ? <AuthenticatedNav /> : <PublicNav />;
}
