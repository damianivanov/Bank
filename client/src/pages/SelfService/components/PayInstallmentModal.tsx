import { CreditCard } from "lucide-react";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { Dropdown, Modal } from "@/shared/components";
import type { CreditDetails } from "@/types";
import { usePayInstallmentModal } from "../hooks/usePayInstallmentModal";

type PayInstallmentModalProps = {
  isOpen: boolean;
  credit: CreditDetails | null;
  onClose: () => void;
  onPaid: () => void;
};

export default function PayInstallmentModal({ isOpen, credit, onClose, onPaid }: PayInstallmentModalProps) {
  const { state, actions } = usePayInstallmentModal({ isOpen, credit, onClose, onPaid });

  if (!credit) {
    return null;
  }

  const installment = state.nextInstallment;
  const hasMultipleAccounts = state.accounts.length > 1;

  return (
    <Modal title="Плащане на месечна вноска" isOpen={isOpen} onClose={actions.close}>
      {!installment ? (
        <p className="text-sm text-secondary">Този кредит няма предстояща вноска за плащане.</p>
      ) : (
        <form onSubmit={actions.submit}>
          <div className="bank-panel mb-4 rounded-xl p-4">
            <div className="flex items-center justify-between text-sm">
              <span className="text-secondary">Вноска №{installment.paymentNumber}</span>
              <span className="font-semibold">{formatCurrency(installment.paymentAmount)}</span>
            </div>
            <p className="mt-1 text-xs text-tertiary">Падеж: {formatDate(installment.dueDate)}</p>
          </div>

          {state.isLoadingAccounts ? (
            <p className="text-sm text-secondary">Зареждане на сметките...</p>
          ) : state.accountsError ? (
            <p className="text-sm font-semibold text-rose-500">{state.accountsError}</p>
          ) : state.accounts.length === 0 ? (
            <p className="text-sm font-semibold text-rose-500">
              Нямате активна сметка, от която да платите тази вноска.
            </p>
          ) : (
            <>
              {hasMultipleAccounts ? (
                <Dropdown
                  label="Плати от сметка"
                  name="fundingAccountId"
                  value={state.selectedAccountId}
                  onChange={actions.handleAccountChange}
                  required
                  options={state.accounts.map((account) => ({
                    value: String(account.id),
                    label: `${account.iban} — ${formatCurrency(account.balance)}`,
                  }))}
                />
              ) : (
                <p className="text-sm text-secondary">
                  Плащане от сметка <span className="font-mono text-xs">{state.accounts[0].iban}</span>{" "}
                  (салдо {formatCurrency(state.accounts[0].balance)}).
                </p>
              )}

              <button
                type="submit"
                disabled={state.isSubmitting}
                className="bank-primary-btn mt-5 bank-btn disabled:opacity-60"
              >
                <CreditCard className="h-4 w-4" />
                {state.isSubmitting ? "Плащане..." : "Плати вноската"}
              </button>
            </>
          )}
        </form>
      )}
    </Modal>
  );
}
