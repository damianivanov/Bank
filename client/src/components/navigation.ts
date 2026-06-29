import type { LucideIcon } from "lucide-react";
import { adminRoles, customerRoles, staffWorkspaceRoles } from "@/lib/access";
import type { UserRole } from "@/types";
import {
  BadgeCheck,
  Banknote,
  Calculator,
  Landmark,
  LayoutDashboard,
  Settings2,
  TriangleAlert,
  UserCheck,
  UsersRound,
  Wallet,
} from "lucide-react";

export type NavItem = {
  label: string;
  to: string;
  icon: LucideIcon;
  end?: boolean;
  allowedRoles?: readonly UserRole[];
};

export type NavSection = {
  section: string;
  items: NavItem[];
};

export type MobileNavItem = Pick<NavItem, "label" | "to" | "icon" | "end" | "allowedRoles"> & {
  isPrimaryAction?: boolean;
};

const appNavItems: NavItem[] = [
  { label: "Табло", to: "/dashboard", icon: LayoutDashboard, end: true },
  {
    label: "Моето банкиране",
    to: "/my-banking",
    icon: Wallet,
    allowedRoles: customerRoles,
  },
  { label: "Калкулатори", to: "/calculators", icon: Calculator },
  {
    label: "Клиенти",
    to: "/customers",
    icon: UserCheck,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
  {
    label: "Кредити",
    to: "/credits",
    icon: Banknote,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
];

const managementNavItems: NavItem[] = [
  {
    label: "Потребители",
    to: "/all-users",
    icon: UsersRound,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
  {
    label: "Заявки за депозит",
    to: "/management/deposit-approvals",
    icon: BadgeCheck,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
  {
    label: "Кредитни условия",
    to: "/management/credit-conditions",
    icon: Settings2,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
  {
    label: "Всички потребители",
    to: "/management/users",
    icon: UsersRound,
    end: true,
    allowedRoles: adminRoles,
  },
  {
    label: "Грешки",
    to: "/management/errors",
    icon: TriangleAlert,
    end: true,
    allowedRoles: adminRoles,
  },
];

export const navSections: NavSection[] = [
  { section: "Работна среда", items: appNavItems },
  { section: "Администрация", items: managementNavItems },
];

export const mobileBottomNavItems: MobileNavItem[] = [
  { label: "Табло", to: "/dashboard", icon: Landmark, end: true },
  { label: "Моето банкиране", to: "/my-banking", icon: Wallet, allowedRoles: customerRoles },
];
