import { formatCurrency, formatDate } from "@/lib/formatters";
import { AccountIbanCard, Modal, MoneyTransactionTypeBadge, Pagination } from "@/shared/components";
import { MoneyTransactionType, type CustomerAccountSummary } from "@/types";
import { useAccountHistoryModal } from "../hooks/useAccountHistoryModal";

type AccountHistoryModalProps = {
  isOpen: boolean;
  account: CustomerAccountSummary | null;
  onClose: () => void;
};

function signedAmount(type: MoneyTransactionType, amount: number): string {
  const formatted = formatCurrency(amount);
  return type === MoneyTransactionType.Deposit ? `+${formatted}` : `−${formatted}`;
}

function movementsLabel(count: number): string {
  return count === 1 ? "1 движение" : `${count} движения`;
}

export default function AccountHistoryModal({ isOpen, account, onClose }: AccountHistoryModalProps) {
  const { state, actions } = useAccountHistoryModal({ isOpen, account });

  if (!account) {
    return null;
  }

  return (
    <Modal title="Движения по сметката" isOpen={isOpen} onClose={onClose}>
      <AccountIbanCard iban={account.iban} className="mb-5" />

      {state.isLoading ? (
        <p className="text-sm text-secondary">Зареждане на движенията...</p>
      ) : state.error ? (
        <p className="text-sm font-semibold text-rose-500">{state.error}</p>
      ) : state.totalCount === 0 ? (
        <p className="text-sm text-secondary">Все още няма движения по тази сметка.</p>
      ) : (
        <>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-tertiary">
            {movementsLabel(state.totalCount)}
          </p>
          <ul className="bank-scrollbar max-h-96 space-y-1.5 overflow-y-auto pr-1">
            {state.transactions.map((transaction) => {
              const isDeposit = transaction.type === MoneyTransactionType.Deposit;
              return (
                <li
                  key={transaction.id}
                  className="flex items-center justify-between gap-3 rounded-xl border border-black/10 bg-black/[0.02] px-3.5 py-3 dark:border-white/10 dark:bg-white/[0.03]"
                >
                  <div className="min-w-0">
                    <MoneyTransactionTypeBadge type={transaction.type} />
                    <p className="mt-1 text-xs tabular-nums text-tertiary">{formatDate(transaction.dateCreated)}</p>
                  </div>
                  <div className="shrink-0 text-right">
                    <p
                      className={`text-base font-bold tabular-nums ${
                        isDeposit ? "text-accent" : "text-rose-500 dark:text-rose-400"
                      }`}
                    >
                      {signedAmount(transaction.type, transaction.amount)}
                    </p>
                    <p className="mt-0.5 text-xs tabular-nums text-tertiary">
                      Салдо: {formatCurrency(transaction.balanceAfter)}
                    </p>
                  </div>
                </li>
              );
            })}
          </ul>
        </>
      )}

      {state.totalCount > 0 && !state.error ? (
        <Pagination
          page={state.page}
          pageSize={state.pageSize}
          totalCount={state.totalCount}
          onPageChange={actions.goToPage}
        />
      ) : null}
    </Modal>
  );
}
