import { useCallback, useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { myBankingService } from "@/services/myBankingService";
import type { CustomerAccountSummary, MoneyTransaction } from "@/types";

const PAGE_SIZE = 10;

type UseAccountHistoryModalArgs = {
  isOpen: boolean;
  account: CustomerAccountSummary | null;
};

export function useAccountHistoryModal({ isOpen, account }: UseAccountHistoryModalArgs) {
  const [transactions, setTransactions] = useState<MoneyTransaction[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const accountId = account?.id ?? null;

  // При отваряне на модала или смяна на сметката започваме винаги от първата страница.
  useEffect(() => {
    setPage(1);
  }, [isOpen, accountId]);

  useEffect(() => {
    if (!isOpen || accountId === null) {
      return;
    }

    const targetAccountId = accountId;
    let isCancelled = false;

    async function loadTransactions() {
      setIsLoading(true);
      setError(null);
      try {
        const data = await myBankingService.getAccountTransactions(targetAccountId, {
          page,
          pageSize: PAGE_SIZE,
        });
        if (!isCancelled) {
          setTransactions(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setTransactions([]);
          setTotalCount(0);
          setError(getCommonModelErrorMessage(loadError, "Движенията по сметката не бяха заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadTransactions();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, accountId, page]);

  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  const state = useMemo(
    () => ({ transactions, totalCount, page, pageSize: PAGE_SIZE, isLoading, error }),
    [transactions, totalCount, page, isLoading, error],
  );

  const actions = useMemo(() => ({ goToPage }), [goToPage]);

  return { state, actions };
}
