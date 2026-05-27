import { UserRole, type User } from "@/types";

export const adminRoles = [UserRole.Admin] as const;
export const staffRoles = [UserRole.Staff] as const;
export const staffWorkspaceRoles = [UserRole.Staff, UserRole.Admin] as const;
export const customerRoles = [UserRole.Customer] as const;

const roleRank: Record<UserRole, number> = {
  [UserRole.User]: 1,
  [UserRole.Customer]: 2,
  [UserRole.Staff]: 3,
  [UserRole.Admin]: 4,
};

export function hasAnyRole(user: User | null | undefined, roles: readonly UserRole[]): boolean {
  if (!user?.roles?.length) {
    return false;
  }

  return roles.some((role) => user.roles.includes(role));
}

export function hasRoleAtLeast(user: User | null | undefined, minimumRole: UserRole): boolean {
  if (!user?.roles?.length) {
    return false;
  }

  const highestRank = user.roles.reduce((currentRank, role) => {
    const nextRank = roleRank[role] ?? 0;
    return nextRank > currentRank ? nextRank : currentRank;
  }, 0);

  return highestRank >= roleRank[minimumRole];
}

export function isAdmin(user: User | null | undefined): boolean {
  return hasAnyRole(user, adminRoles);
}

export function isStaff(user: User | null | undefined): boolean {
  return hasAnyRole(user, staffRoles);
}

export function isCustomer(user: User | null | undefined): boolean {
  return hasAnyRole(user, customerRoles);
}

export function isStaffOrAdmin(user: User | null | undefined): boolean {
  return hasAnyRole(user, staffWorkspaceRoles);
}
