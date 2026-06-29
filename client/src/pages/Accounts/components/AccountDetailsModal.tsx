import { Ban, Wallet } from "lucide-react";
import { Link } from "react-router-dom";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { AccountStatusBadge, CopyableValue, DetailField, Modal } from "@/shared/components";
import { useAccountDetailsModal } from "../hooks/useAccountDetailsModal";

type AccountDetailsModalProps = {
  isOpen: boolean;
  accountId: number | null;
  onClose: () => void;
  onChanged?: () => void;
};

export default function AccountDetailsModal({ isOpen, accountId, onClose, onChanged }: AccountDetailsModalProps) {
  const { state, actions } = useAccountDetailsModal({ accountId, isOpen, onClose, onChanged });
  const account = state.account;

  return (
    <Modal title="Детайли за сметка" isOpen={isOpen} onClose={onClose} widthClassName="max-w-xl">
      {state.isLoading || !account ? (
        <p className="text-sm text-secondary">Зареждане на сметка...</p>
      ) : (
        <div className="space-y-5">
          {/* Идентичност на сметката: IBAN (с копиране) + статус */}
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3.5">
              <span className="bank-icon-tile-soft flex h-11 w-11 shrink-0 items-center justify-center rounded-xl">
                <Wallet className="h-5 w-5" />
              </span>
              <div className="min-w-0">
                <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">IBAN</p>
                <CopyableValue value={account.iban} label="IBAN" className="text-base sm:text-lg" />
              </div>
            </div>
            <AccountStatusBadge status={account.status} />
          </div>

          {/* Салдо */}
          <div className="rounded-xl border border-black/10 p-4 dark:border-white/10">
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Салдо</p>
            <p className="mt-1 text-3xl font-bold tracking-tight tabular-nums text-accent">
              {formatCurrency(account.balance)}
            </p>
          </div>

          {/* Метаданни */}
          <div className="grid gap-5 sm:grid-cols-3">
            <DetailField label="Клиент" valueClassName="font-semibold">
              <Link to={`/customers/${account.customerId}`} className="bank-accent-link hover:underline!">
                {account.customerDisplayName}
              </Link>
            </DetailField>
            <DetailField label="Открита на">{formatDate(account.openedAtUtc)}</DetailField>
            <DetailField label="Закрита на">{formatDate(account.closedAtUtc)}</DetailField>
          </div>

          {/* Действия */}
          <div className="flex justify-end border-t border-black/10 pt-4 dark:border-white/10">
            <button
              type="button"
              onClick={actions.closeAccount}
              disabled={!state.canClose || state.isClosing}
              className="bank-danger-btn bank-btn disabled:cursor-not-allowed disabled:opacity-60"
            >
              <Ban className="h-4 w-4" />
              {state.isClosing ? "Закриване..." : "Закрий сметка"}
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
}
