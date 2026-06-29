import { Send } from "lucide-react";
import { formatCurrency } from "@/lib/formatters";
import { AccountIbanCard, Modal, MoneyInputField } from "@/shared/components";
import type { CustomerAccountSummary } from "@/types";
import { useDepositRequestModal } from "../hooks/useDepositRequestModal";

type DepositRequestModalProps = {
  isOpen: boolean;
  account: CustomerAccountSummary | null;
  onClose: () => void;
  onSubmitted: () => void;
};

export default function DepositRequestModal({ isOpen, account, onClose, onSubmitted }: DepositRequestModalProps) {
  const { state, actions } = useDepositRequestModal({ isOpen, account, onClose, onSubmitted });

  if (!account) {
    return null;
  }

  return (
    <Modal title="Заявка за депозит" isOpen={isOpen} onClose={actions.close}>
      <AccountIbanCard iban={account.iban} className="mb-4" />
      <p className="mb-4 text-xs text-tertiary">
        Депозитът се одобрява от служител — салдото се увеличава едва след одобрение.
      </p>

      <form onSubmit={actions.submit}>
        <MoneyInputField
          label="Сума за депозит"
          name="amount"
          value={state.amount}
          onValueChange={actions.setAmount}
          error={state.error}
          autoFocus
        />

        <p className="mt-3 text-xs text-tertiary">Текущо салдо: {formatCurrency(account.balance)}</p>

        <div className="mt-5 flex justify-end">
          <button
            type="submit"
            disabled={state.isSubmitting}
            className="bank-primary-btn bank-btn disabled:opacity-60"
          >
            <Send className="h-4 w-4" />
            {state.isSubmitting ? "Подаване..." : "Подай заявка"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
