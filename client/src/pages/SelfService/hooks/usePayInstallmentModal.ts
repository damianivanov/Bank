import { useCallback, useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { newIdempotencyKey } from "@/lib/idempotency";
import { myBankingService } from "@/services/myBankingService";
import {
  BankAccountStatus,
  CreditPaymentStatus,
  type CreditDetails,
  type CustomerAccountSummary,
} from "@/types";

type UsePayInstallmentModalArgs = {
  isOpen: boolean;
  credit: CreditDetails | null;
  onClose: () => void;
  onPaid: () => void;
};

export function usePayInstallmentModal({ isOpen, credit, onClose, onPaid }: UsePayInstallmentModalArgs) {
  const [accounts, setAccounts] = useState<CustomerAccountSummary[]>([]);
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const [isLoadingAccounts, setIsLoadingAccounts] = useState(false);
  const [accountsError, setAccountsError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const nextInstallment = useMemo(
    () => credit?.payments.find((payment) => payment.status === CreditPaymentStatus.Pending) ?? null,
    [credit],
  );

  useEffect(() => {
    if (!isOpen || !credit) {
      return;
    }

    let isCancelled = false;
    const customerId = credit.customerId;

    async function loadAccounts() {
      setIsLoadingAccounts(true);
      setAccountsError(null);
      try {
        const profile = await myBankingService.getProfile(customerId);
        if (isCancelled) {
          return;
        }
        const activeAccounts = profile.accounts.filter((account) => account.status === BankAccountStatus.Active);
        setAccounts(activeAccounts);
        // Авто-избор, ако има точно една активна сметка; при няколко — клиентът избира.
        setSelectedAccountId(activeAccounts.length === 1 ? String(activeAccounts[0].id) : "");
      } catch (loadError) {
        if (!isCancelled) {
          setAccounts([]);
          setSelectedAccountId("");
          setAccountsError(getCommonModelErrorMessage(loadError, "Сметките ви не бяха заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoadingAccounts(false);
        }
      }
    }

    void loadAccounts();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, credit]);

  const handleAccountChange = useCallback((event: ChangeEvent<HTMLSelectElement>) => {
    setSelectedAccountId(event.target.value);
  }, []);

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }
    onClose();
  }, [isSubmitting, onClose]);

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!credit || !nextInstallment) {
        return;
      }

      if (!selectedAccountId) {
        toast.error("Изберете сметка, от която да платите вноската");
        return;
      }

      setIsSubmitting(true);
      try {
        const result = await myBankingService.payCreditInstallment(credit.id, {
          fundingAccountId: Number(selectedAccountId),
          idempotencyKey: newIdempotencyKey(),
        });
        toast.success(`Вноската е платена. Ново салдо: ${result.newBalance.toFixed(2)}`);
        onPaid();
        onClose();
      } catch (submitError) {
        toast.error(getCommonModelErrorMessage(submitError, "Вноската не бе платена"));
      } finally {
        setIsSubmitting(false);
      }
    },
    [credit, nextInstallment, onClose, onPaid, selectedAccountId],
  );

  const state = useMemo(
    () => ({ accounts, selectedAccountId, isLoadingAccounts, accountsError, isSubmitting, nextInstallment }),
    [accounts, selectedAccountId, isLoadingAccounts, accountsError, isSubmitting, nextInstallment],
  );
  const actions = useMemo(() => ({ handleAccountChange, submit, close }), [handleAccountChange, submit, close]);

  return { state, actions };
}
