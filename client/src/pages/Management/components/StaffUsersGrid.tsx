import { EntityGrid } from "@/shared/components";
import type { StaffUserGrid } from "@/types";
import { formatUserName } from "../utils/userAccess.utils";
import { PersonDisplay, UserStatusBadge } from "./UserGridCells";
import UserRoleBadges from "./UserRoleBadges";

type StaffUsersGridProps = {
  users: StaffUserGrid[];
  isLoading: boolean;
  onOpenUser: (userId: number) => void;
};

export default function StaffUsersGrid({ users, isLoading, onOpenUser }: StaffUsersGridProps) {
  return (
    <>
      <div className="md:hidden">
        {isLoading ? (
          <p className="text-sm text-secondary">Зареждане на потребители...</p>
        ) : users.length === 0 ? (
          <p className="text-sm text-secondary">Няма потребители, отговарящи на търсенето.</p>
        ) : (
          <div className="space-y-3">
            {users.map((user) => (
              <article key={user.id} className="bank-panel rounded-2xl p-4">
                <button
                  type="button"
                  onClick={() => onOpenUser(user.id)}
                  className="min-w-0 cursor-pointer text-left text-sm font-semibold text-foreground underline-offset-4 transition hover:underline"
                >
                  {user.email}
                </button>

                <p className="mt-2 text-sm text-secondary">{formatUserName(user)}</p>

                <div className="mt-3">
                  <UserRoleBadges user={user} />
                </div>

                <div className="mt-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Лице</p>
                  <p className="mt-1 text-sm">
                    <PersonDisplay personDisplayName={user.personDisplayName} />
                  </p>
                </div>

                <div className="mt-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Статус</p>
                  <div className="mt-1">
                    <UserStatusBadge isActive={user.isActive} />
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>

      <div className="hidden md:block">
        {isLoading ? (
          <p className="text-sm text-secondary">Зареждане на потребители...</p>
        ) : (
          <EntityGrid>
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
                <th className="px-4 py-3 text-left">Имейл</th>
                <th className="px-4 py-3">Име</th>
                <th className="px-4 py-3">Лице</th>
                <th className="px-4 py-3">Роли</th>
                <th className="px-4 py-3">Статус</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b border-slate-100 text-sm last:border-b-0">
                  <td className="px-4 py-3 font-medium">
                    <button
                      type="button"
                      onClick={() => onOpenUser(user.id)}
                      className="cursor-pointer text-left text-foreground underline-offset-4 transition hover:underline"
                    >
                      {user.email}
                    </button>
                  </td>
                  <td className="px-4 py-3">{formatUserName(user)}</td>
                  <td className="px-4 py-3">
                    <PersonDisplay personDisplayName={user.personDisplayName} />
                  </td>
                  <td className="px-4 py-3">
                    <UserRoleBadges user={user} />
                  </td>
                  <td className="px-4 py-3">
                    <UserStatusBadge isActive={user.isActive} />
                  </td>
                </tr>
              ))}
              {users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-secondary">
                    Няма потребители, отговарящи на търсенето.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </EntityGrid>
        )}
      </div>
    </>
  );
}
