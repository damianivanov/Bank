import { UserCheck, UserX } from "lucide-react";
import { EntityGrid } from "@/shared/components";
import { formatUserName, type AccessOptionKey, type EditableUserAccess } from "../utils/userAccess.utils";
import AccessDropdown from "./AccessDropdown";
import { UserStatusBadge } from "./UserGridCells";
import UserRoleBadges from "./UserRoleBadges";

type AdminUsersGridProps = {
  users: EditableUserAccess[];
  isLoading: boolean;
  canManageAccess: boolean;
  isUserSaving: (userId: number) => boolean;
  onOpenUser: (userId: number) => void;
  onToggleAccessOption: (userId: number, key: AccessOptionKey, nextValue: boolean) => void;
  onToggleActive: (userId: number, isActive: boolean) => void;
};

function getActiveActionLabel(isActive: boolean, isRowLoading: boolean) {
  if (isRowLoading) {
    return "Запазване...";
  }

  return isActive ? "Деактивирай" : "Активирай";
}

function getActiveActionButtonClassName(isActive: boolean) {
  const variantClassName = isActive ? "bank-danger-btn" : "bank-primary-btn";
  return `${variantClassName} bank-btn-action disabled:cursor-not-allowed disabled:opacity-60`;
}

export default function AdminUsersGrid({
  users,
  isLoading,
  canManageAccess,
  isUserSaving,
  onOpenUser,
  onToggleAccessOption,
  onToggleActive,
}: AdminUsersGridProps) {
  return (
    <>
      <div className="mt-6 md:hidden">
        {isLoading ? (
          <p className="text-sm text-secondary">Зареждане на потребители...</p>
        ) : users.length === 0 ? (
          <p className="text-sm text-secondary">Няма потребители, отговарящи на търсенето.</p>
        ) : (
          <div className="space-y-3">
            {users.map((user) => {
              const isRowLoading = isUserSaving(user.id);
              const isAccessReadOnly = isRowLoading || !canManageAccess;

              return (
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
                    <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Статус</p>
                    <div className="mt-1">
                      <UserStatusBadge isActive={user.isActive} />
                    </div>
                  </div>

                  <div className="mt-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Достъп</p>
                    <div className="mt-1">
                      <AccessDropdown
                        disabled={isAccessReadOnly}
                        values={{ isActive: user.isActive, isStaff: user.isStaff, isAdmin: user.isAdmin }}
                        onToggleOption={(key, nextValue) => onToggleAccessOption(user.id, key, nextValue)}
                      />
                    </div>
                  </div>

                  <div className="mt-3">
                    <button
                      type="button"
                      disabled={isAccessReadOnly}
                      onClick={() => onToggleActive(user.id, user.isActive)}
                      className={getActiveActionButtonClassName(user.isActive)}
                    >
                      {user.isActive ? <UserX className="h-3.5 w-3.5" /> : <UserCheck className="h-3.5 w-3.5" />}
                      {getActiveActionLabel(user.isActive, isRowLoading)}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </div>

      <div className="mt-6 hidden md:block">
        {isLoading ? (
          <p className="text-sm text-secondary">Зареждане на потребители...</p>
        ) : (
          <EntityGrid>
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
                <th className="px-4 py-3 text-left">Имейл</th>
                <th className="px-4 py-3">Име</th>
                <th className="px-4 py-3">Роли</th>
                <th className="px-4 py-3">Статус</th>
                <th className="px-4 py-3">Достъп</th>
                <th className="px-4 py-3">Действия</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => {
                const isRowLoading = isUserSaving(user.id);
                const isAccessReadOnly = isRowLoading || !canManageAccess;

                return (
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
                      <UserRoleBadges user={user} />
                    </td>
                    <td className="px-4 py-3">
                      <UserStatusBadge isActive={user.isActive} />
                    </td>
                    <td className="px-4 py-3">
                      <div className="w-56">
                        <AccessDropdown
                          disabled={isAccessReadOnly}
                          values={{ isActive: user.isActive, isStaff: user.isStaff, isAdmin: user.isAdmin }}
                          onToggleOption={(key, nextValue) => onToggleAccessOption(user.id, key, nextValue)}
                        />
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        disabled={isAccessReadOnly}
                        onClick={() => onToggleActive(user.id, user.isActive)}
                        className={getActiveActionButtonClassName(user.isActive)}
                      >
                        {user.isActive ? <UserX className="h-3.5 w-3.5" /> : <UserCheck className="h-3.5 w-3.5" />}
                        {getActiveActionLabel(user.isActive, isRowLoading)}
                      </button>
                    </td>
                  </tr>
                );
              })}
              {users.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-secondary">
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
