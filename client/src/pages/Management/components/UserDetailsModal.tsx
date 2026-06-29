import { UserPlus } from "lucide-react";
import { DetailField, Modal } from "@/shared/components";
import {
  formatRole,
  formatUserName,
  getRoleBadgeClassName,
  type EditableUserAccess,
} from "../utils/userAccess.utils";

type UserDetailsModalProps = {
  user: EditableUserAccess | null;
  onClose: () => void;
  onCreateCustomer: (userId: number) => void;
};

/** Инициали за аватара — име/фамилия, с резерв към имейла. */
function userInitials(user: EditableUserAccess): string {
  const fromName = `${user.firstName?.charAt(0) ?? ""}${user.lastName?.charAt(0) ?? ""}`.trim();
  return (fromName || user.email.charAt(0)).toUpperCase() || "?";
}

export default function UserDetailsModal({ user, onClose, onCreateCustomer }: UserDetailsModalProps) {
  const hasPerson = Boolean(user?.personId);
  // Администратор/служител не може да има клиентски акаунт, затова не предлагаме създаване на клиент.
  const isStaffOrAdmin = Boolean(user && (user.isAdmin || user.isStaff));

  return (
    <Modal
      isOpen={user != null}
      onClose={onClose}
      title="Детайли за потребителя"
      widthClassName="max-w-2xl"
    >
      {user ? (
        <div className="space-y-5">
          {/* Идентичност: аватар + име/имейл + статус — както при сметки и клиенти. */}
          <div className="overflow-hidden rounded-2xl border border-black/10 dark:border-white/10">
            <div className="flex flex-wrap items-center justify-between gap-4 bg-black/[0.02] p-5 sm:p-6 dark:bg-white/[0.03]">
              <div className="flex min-w-0 items-center gap-3.5">
                <span className="bank-icon-tile-soft flex h-12 w-12 shrink-0 items-center justify-center rounded-xl text-base font-bold">
                  {userInitials(user)}
                </span>
                <div className="min-w-0">
                  <p className="truncate text-lg font-bold tracking-tight">{formatUserName(user)}</p>
                  <p className="truncate text-sm text-secondary">{user.email}</p>
                </div>
              </div>
              <span
                className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${
                  user.isActive ? "bank-chip-success" : "bank-chip-warn"
                }`}
              >
                <span className="h-1.5 w-1.5 rounded-full bg-current opacity-70" />
                {user.isActive ? "Активен" : "Неактивен"}
              </span>
            </div>

            {/* Метаданни на един ред: ID, свързано лице и роля — без излишен етикет „Роли“. */}
            <div className="grid gap-5 border-t border-black/10 p-5 sm:grid-cols-3 sm:p-6 dark:border-white/10">
              <DetailField label="ID на потребителя" valueClassName="font-semibold">
                {user.id}
              </DetailField>
              <DetailField label="Свързано лице" valueClassName="font-semibold">
                {user.personDisplayName || "Не е свързано"}
              </DetailField>
              <div className="min-w-0">
                {/* Невидим спейсър — подравнява чиповете със стойностите вляво. */}
                <p aria-hidden="true" className="text-xs font-semibold uppercase tracking-wide">
                  &nbsp;
                </p>
                <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
                  {user.roles.map((role) => (
                    <span
                      key={`${user.id}-${role}`}
                      className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${getRoleBadgeClassName(role)}`}
                    >
                      {formatRole(role)}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Подкана за свързване на лице */}
          {!hasPerson ? (
            isStaffOrAdmin ? (
              <div className="rounded-2xl border border-black/10 bg-black/[0.02] px-5 py-4 text-sm text-secondary dark:border-white/10 dark:bg-white/[0.03]">
                Администратор или служител не може да има клиентски акаунт.
              </div>
            ) : (
              <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-black/10 bg-black/[0.02] px-5 py-4 dark:border-white/10 dark:bg-white/[0.03]">
                <p className="text-sm text-secondary">Към този потребител все още не е свързано лице.</p>
                <button
                  type="button"
                  onClick={() => onCreateCustomer(user.id)}
                  className="bank-primary-btn bank-btn-action"
                >
                  <UserPlus className="h-3.5 w-3.5" />
                  Създай клиент
                </button>
              </div>
            )
          ) : null}
        </div>
      ) : null}
    </Modal>
  );
}
