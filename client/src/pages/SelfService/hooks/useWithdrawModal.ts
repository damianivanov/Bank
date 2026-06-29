import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { newIdempotencyKey } from "@/lib/idempotency";
import { validateMoneyAmount } from "@/lib/validation/forms";
import { myBankingService } from "@/services/myBankingService";
import type { CustomerAccountSummary } from "@/types";

type UseWithdrawModalArgs = {
  isOpen: boolean;
  account: CustomerAccountSummary | null;
  onClose: () => void;
  onCompleted: () => void;
};

export function useWithdrawModal({ isOpen, account, onClose, onCompleted }: UseWithdrawModalArgs) {
  const [amount, setAmount] = useState("");
  const [error, setError] = useState<string | undefined>(undefined);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    setAmount("");
    setError(undefined);
    setIsConfirming(false);
  }, [isOpen]);

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }
    onClose();
  }, [isSubmitting, onClose]);

  const submit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!account) {
        return;
      }

      const amountError = validateMoneyAmount(amount);
      if (amountError) {
        setError(amountError);
        return;
      }

      // Клиентска предпроверка за достатъчно салдо — сървърът остава авторитетен под optimistic concurrency.
      if (Number(amount) > account.balance) {
        setError("Недостатъчно салдо за това теглене.");
        return;
      }

      setError(undefined);
      setIsConfirming(true);
    },
    [account, amount],
  );

  const cancelConfirm = useCallback(() => {
    if (isSubmitting) {
      return;
    }
    setIsConfirming(false);
  }, [isSubmitting]);

  const confirm = useCallback(async () => {
    if (!account) {
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await myBankingService.withdraw(account.id, {
        amount: Number(amount),
        idempotencyKey: newIdempotencyKey(),
      });
      toast.success(`Изтеглени са средства. Ново салдо: ${result.newBalance.toFixed(2)}`);
      onCompleted();
      onClose();
    } catch (submitError) {
      toast.error(getCommonModelErrorMessage(submitError, "Тегленето не бе извършено"));
      setIsConfirming(false);
    } finally {
      setIsSubmitting(false);
    }
  }, [account, amount, onClose, onCompleted]);

  const state = useMemo(
    () => ({ amount, error, isSubmitting, isConfirming }),
    [amount, error, isSubmitting, isConfirming],
  );
  const actions = useMemo(
    () => ({ setAmount, submit, confirm, cancelConfirm, close }),
    [submit, confirm, cancelConfirm, close],
  );

  return { state, actions };
}
