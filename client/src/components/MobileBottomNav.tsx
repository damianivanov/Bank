import { NavLink } from "react-router-dom";
import { hasAnyRole } from "@/lib/access";
import type { User } from "@/types";
import { mobileBottomNavItems } from "./navigation";

type MobileBottomNavProps = {
  user: User;
  onNavigate: () => void;
};

const bottomNavLinkClassName =
  "flex h-10 w-10 items-center justify-center border-b border-transparent pb-0.5 text-secondary transition-colors";

const primaryActionClassName =
  "bank-primary-btn flex h-11 w-11 items-center justify-center rounded-full p-0";

function getLinkClassName(isActive: boolean, isPrimaryAction?: boolean) {
  if (isPrimaryAction) {
    return primaryActionClassName;
  }

  return isActive
    ? `${bottomNavLinkClassName} bank-mobile-nav-item-active`
    : bottomNavLinkClassName;
}

export default function MobileBottomNav({ user, onNavigate }: MobileBottomNavProps) {
  const visibleItems = mobileBottomNavItems.filter((item) => {
    if (!item.allowedRoles || item.allowedRoles.length === 0) {
      return true;
    }

    return hasAnyRole(user, item.allowedRoles);
  });

  const gridColumnsStyle = {
    gridTemplateColumns: `repeat(${visibleItems.length}, minmax(0, 1fr))`,
  };

  return (
    <nav aria-label="Mobile primary navigation" className="pointer-events-none fixed inset-x-0 bottom-0 z-40 px-3 pb-3 md:hidden">
      <div className="bank-mobile-bottom-nav pointer-events-auto mx-auto h-14 w-11/12 max-w-lg rounded-full px-3">
        <ul className="grid h-full place-items-center" style={gridColumnsStyle}>
          {visibleItems.map((item) => {
            const Icon = item.icon;

            return (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  onClick={onNavigate}
                  aria-label={item.label}
                  className={({ isActive }) => getLinkClassName(isActive, item.isPrimaryAction)}
                >
                  <Icon className="h-5 w-5" strokeWidth={1.8} />
                </NavLink>
              </li>
            );
          })}
        </ul>
      </div>
    </nav>
  );
}
