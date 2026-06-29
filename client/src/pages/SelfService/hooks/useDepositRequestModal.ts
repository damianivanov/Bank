import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { newIdempotencyKey } from "@/lib/idempotency";
import { validateMoneyAmount } from "@/lib/validation/forms";
import { myBankingService } from "@/services/myBankingService";
import type { CustomerAccountSummary } from "@/types";

type UseDepositRequestModalArgs = {
  isOpen: boolean;
  account: CustomerAccountSummary | null;
  onClose: () => void;
  onSubmitted: () => void;
};

export function useDepositRequestModal({ isOpen, account, onClose, onSubmitted }: UseDepositRequestModalArgs) {
  const [amount, setAmount] = useState("");
  const [error, setError] = useState<string | undefined>(undefined);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    setAmount("");
    setError(undefined);
  }, [isOpen]);

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }
    onClose();
  }, [isSubmitting, onClose]);

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!account) {
        return;
      }

      const amountError = validateMoneyAmount(amount);
      setError(amountError);
      if (amountError) {
        return;
      }

      setIsSubmitting(true);
      try {
        await myBankingService.requestDeposit(account.id, {
          amount: Number(amount),
          idempotencyKey: newIdempotencyKey(),
        });
        toast.success("Заявката за депозит е подадена и чака одобрение от служител");
        onSubmitted();
        onClose();
      } catch (submitError) {
        toast.error(getCommonModelErrorMessage(submitError, "Заявката за депозит не бе подадена"));
      } finally {
        setIsSubmitting(false);
      }
    },
    [account, amount, onClose, onSubmitted],
  );

  const state = useMemo(() => ({ amount, error, isSubmitting }), [amount, error, isSubmitting]);
  const actions = useMemo(() => ({ setAmount, submit, close }), [submit, close]);

  return { state, actions };
}
