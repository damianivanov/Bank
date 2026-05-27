import { formatRole, getRoleBadgeClassName } from "./userAccess.utils";
import type { UserRole } from "@/types";

type UserWithRoles = {
  id: number;
  roles: UserRole[];
};

type UserRoleBadgesProps = {
  user: UserWithRoles;
};

export default function UserRoleBadges({ user }: UserRoleBadgesProps) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {user.roles.map((role) => (
        <span
          key={`${user.id}-${role}`}
          className={`bank-chip ${getRoleBadgeClassName(role)} rounded-full px-2 py-0.5 text-xs font-semibold`}
        >
          {formatRole(role)}
        </span>
      ))}
    </div>
  );
}
