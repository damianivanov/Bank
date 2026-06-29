import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { accountService } from "@/services/accountService";
import { BankAccountStatus, type BankAccountDetails } from "@/types";

type UseAccountDetailsModalArgs = {
  accountId: number | null;
  isOpen: boolean;
  onClose: () => void;
  onChanged?: () => void;
};

export function useAccountDetailsModal({ accountId, isOpen, onClose, onChanged }: UseAccountDetailsModalArgs) {
  const [account, setAccount] = useState<BankAccountDetails | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isClosing, setIsClosing] = useState(false);

  // Държим onClose в ref, за да зависи зареждащият ефект само от (accountId, isOpen)
  // и да не се преизпълнява при всеки render на родителя с inline callback.
  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (!isOpen) {
      setAccount(null);
      return;
    }

    if (accountId === null || !Number.isFinite(accountId) || accountId <= 0) {
      toast.error("Невалиден идентификатор на сметка");
      onCloseRef.current();
      return;
    }

    let active = true;
    setAccount(null);
    setIsLoading(true);

    void (async () => {
      try {
        const accountDetails = await accountService.getAccount(accountId);
        if (active) {
          setAccount(accountDetails);
        }
      } catch (error) {
        if (active) {
          toast.error(getCommonModelErrorMessage(error, "Сметката не може да бъде заредена"));
          onCloseRef.current();
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [accountId, isOpen]);

  const closeAccount = useCallback(async () => {
    if (!account) {
      return;
    }

    setIsClosing(true);
    try {
      const closedAccount = await accountService.closeAccount(account.id);
      setAccount(closedAccount);
      onChanged?.();
      toast.success("Сметката е закрита");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Сметката не може да бъде закрита"));
    } finally {
      setIsClosing(false);
    }
  }, [account, onChanged]);

  const canClose = account?.status === BankAccountStatus.Active && account.balance === 0;

  const state = useMemo(
    () => ({ account, isLoading, isClosing, canClose }),
    [account, isLoading, isClosing, canClose],
  );
  const actions = useMemo(() => ({ closeAccount }), [closeAccount]);

  return { state, actions };
}
