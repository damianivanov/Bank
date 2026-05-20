import { UserRole, type User } from "@/types";

export function hasAnyRole(user: User | null | undefined, roles: readonly UserRole[]): boolean {
  if (!user?.roles?.length) {
    return false;
  }

  return roles.some((role) => user.roles.includes(role));
}

export function isAdmin(user: User | null | undefined): boolean {
  return hasAnyRole(user, [UserRole.Admin]);
}
