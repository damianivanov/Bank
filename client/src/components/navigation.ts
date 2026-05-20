import type { LucideIcon } from "lucide-react";
import {
  CircleUserRound,
  Landmark,
  LayoutDashboard,
} from "lucide-react";

export type NavItem = {
  label: string;
  to: string;
  icon: LucideIcon;
  end?: boolean;
  requiresAdmin?: boolean;
};

export type NavSection = {
  section: string;
  items: NavItem[];
};

export type MobileNavItem = Pick<NavItem, "label" | "to" | "icon" | "end"> & {
  isPrimaryAction?: boolean;
};

const appNavItems: NavItem[] = [
  { label: "Dashboard", to: "/dashboard", icon: LayoutDashboard, end: true },
  { label: "Profile", to: "/profile", icon: CircleUserRound, end: true },
];

export const navSections: NavSection[] = [
  { section: "Phase 1", items: appNavItems },
];

export const mobileBottomNavItems: MobileNavItem[] = [
  { label: "Dashboard", to: "/dashboard", icon: Landmark, end: true },
  { label: "Profile", to: "/profile", icon: CircleUserRound, end: true },
];
