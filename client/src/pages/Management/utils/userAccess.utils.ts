import { UserRole, type UserAccess } from "@/types";

export type EditableUserAccess = UserAccess & {
  isStaff: boolean;
  isAdmin: boolean;
};

export type UserAccessPatch = {
  isActive: boolean;
  isStaff: boolean;
  isAdmin: boolean;
};

export type AccessOptionKey = keyof UserAccessPatch;

export const accessOptionLabels: Record<AccessOptionKey, string> = {
  isActive: "Активен",
  isStaff: "Служител",
  isAdmin: "Администратор",
};

export function createEditableUser(user: UserAccess): EditableUserAccess {
  return {
    ...user,
    isStaff: user.roles.includes(UserRole.Staff),
    isAdmin: user.roles.includes(UserRole.Admin),
  };
}

export function formatUserName(user: Pick<UserAccess, "firstName" | "lastName">): string {
  return [user.firstName, user.lastName].filter(Boolean).join(" ").trim() || "-";
}

export function formatRole(role: UserRole): string {
  switch (role) {
    case UserRole.Admin:
      return "Администратор";
    case UserRole.Staff:
      return "Служител";
    case UserRole.Customer:
      return "Клиент";
    default:
      return "Потребител";
  }
}

export function getRoleBadgeClassName(role: UserRole): string {
  switch (role) {
    case UserRole.Admin:
      return "bank-chip-danger";
    case UserRole.Staff:
      return "bank-chip-info";
    case UserRole.Customer:
      return "bank-chip-success";
    default:
      return "bank-chip-user";
  }
}

export function getUserAccessPatch(user: Pick<EditableUserAccess, "isActive" | "isStaff" | "isAdmin">): UserAccessPatch {
  return {
    isActive: user.isActive,
    isStaff: user.isStaff,
    isAdmin: user.isAdmin,
  };
}

export function isCustomerUser(user: Pick<EditableUserAccess, "roles" | "personId">): boolean {
  return Boolean(user.personId) || user.roles.includes(UserRole.Customer);
}

export function isAdminGridUser(user: Pick<EditableUserAccess, "isAdmin" | "isStaff" | "roles" | "personId">): boolean {
  return user.isAdmin || user.isStaff || !isCustomerUser(user);
}
