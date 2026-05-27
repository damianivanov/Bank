import type { LucideIcon } from "lucide-react";
import { adminRoles, staffWorkspaceRoles } from "@/lib/access";
import type { UserRole } from "@/types";
import {
  Landmark,
  LayoutDashboard,
  Settings2,
  UsersRound,
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
  { label: "Dashboard", to: "/dashboard", icon: LayoutDashboard, end: true },
  {
    label: "Users",
    to: "/users",
    icon: UsersRound,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
];

const managementNavItems: NavItem[] = [
  {
    label: "Credit Conditions",
    to: "/management/credit-conditions",
    icon: Settings2,
    end: true,
    allowedRoles: staffWorkspaceRoles,
  },
  {
    label: "Admin Users",
    to: "/management/users/admin",
    icon: UsersRound,
    end: true,
    allowedRoles: adminRoles,
  },
];

export const navSections: NavSection[] = [
  { section: "Workspace", items: appNavItems },
  { section: "Management", items: managementNavItems },
];

export const mobileBottomNavItems: MobileNavItem[] = [
  { label: "Dashboard", to: "/dashboard", icon: Landmark, end: true },
];
