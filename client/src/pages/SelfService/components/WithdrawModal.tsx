import { ArrowUpFromLine } from "lucide-react";
import { formatCurrency } from "@/lib/formatters";
import { AccountIbanCard, Modal, MoneyInputField } from "@/shared/components";
import type { CustomerAccountSummary } from "@/types";
import { useWithdrawModal } from "../hooks/useWithdrawModal";

type WithdrawModalProps = {
  isOpen: boolean;
  account: CustomerAccountSummary | null;
  onClose: () => void;
  onCompleted: () => void;
};

export default function WithdrawModal({ isOpen, account, onClose, onCompleted }: WithdrawModalProps) {
  const { state, actions } = useWithdrawModal({ isOpen, account, onClose, onCompleted });

  if (!account) {
    return null;
  }

  const amountValue = Number(state.amount);
  const remaining = account.balance - amountValue;

  return (
    <Modal
      title={state.isConfirming ? "Потвърждение на теглене" : "Теглене на средства"}
      isOpen={isOpen}
      onClose={actions.close}
    >
      <AccountIbanCard iban={account.iban} className="mb-4" />

      {state.isConfirming ? (
        <>
          <p className="mb-4 text-sm text-secondary">Сигурни ли сте, че искате да изтеглите тази сума?</p>

          <div className="space-y-3">
            <div className="flex items-center justify-between gap-3 rounded-xl border border-black/10 bg-black/[0.02] px-3.5 py-3 dark:border-white/10 dark:bg-white/[0.03]">
              <span className="text-xs font-semibold uppercase tracking-wide text-tertiary">За теглене</span>
              <span className="text-base font-bold tabular-nums text-rose-500 dark:text-rose-400">
                −{formatCurrency(amountValue)}
              </span>
            </div>
            <div className="flex items-center justify-between gap-3 rounded-xl border border-black/10 bg-black/[0.02] px-3.5 py-3 dark:border-white/10 dark:bg-white/[0.03]">
              <span className="text-xs font-semibold uppercase tracking-wide text-tertiary">Оставащо салдо</span>
              <span className="text-base font-bold tabular-nums">{formatCurrency(remaining)}</span>
            </div>
          </div>

          <div className="mt-5 flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={actions.cancelConfirm}
              disabled={state.isSubmitting}
              className="bank-secondary-btn bank-btn disabled:opacity-60"
            >
              Назад
            </button>
            <button
              type="button"
              onClick={actions.confirm}
              disabled={state.isSubmitting}
              className="bank-primary-btn bank-btn disabled:opacity-60"
            >
              <ArrowUpFromLine className="h-4 w-4" />
              {state.isSubmitting ? "Теглене..." : "Потвърди тегленето"}
            </button>
          </div>
        </>
      ) : (
        <>
          <p className="mb-4 text-xs text-tertiary">Тегленето е незабавно и зависи от наличното салдо.</p>

          <form onSubmit={actions.submit}>
            <MoneyInputField
              label="Сума за теглене"
              name="amount"
              value={state.amount}
              onValueChange={actions.setAmount}
              error={state.error}
              autoFocus
            />

            <p className="mt-3 text-xs text-tertiary">Налично салдо: {formatCurrency(account.balance)}</p>

            <div className="mt-5 flex justify-end">
              <button type="submit" className="bank-primary-btn bank-btn">
                <ArrowUpFromLine className="h-4 w-4" />
                Изтегли
              </button>
            </div>
          </form>
        </>
      )}
    </Modal>
  );
}
