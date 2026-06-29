import { useCallback, useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { accountService } from "@/services/accountService";
import type { BankAccount } from "@/types";

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

export function useAccountsListPage() {
  const [accounts, setAccounts] = useState<BankAccount[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadIndex, setReloadIndex] = useState(0);

  // При писане в полето изчакваме кратко, нулираме страницата и едва тогава прилагаме търсенето,
  // за да не пращаме заявка при всеки натиснат клавиш.
  useEffect(() => {
    const handle = window.setTimeout(() => {
      setAppliedSearch(searchInput.trim());
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    let isCancelled = false;

    async function loadAccounts() {
      setIsLoading(true);
      setError(null);

      try {
        const data = await accountService.getAccounts({
          page,
          pageSize: PAGE_SIZE,
          search: appliedSearch || undefined,
        });
        if (!isCancelled) {
          setAccounts(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setAccounts([]);
          setTotalCount(0);
          setError(getCommonModelErrorMessage(loadError, "Сметките не могат да бъдат заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadAccounts();

    return () => {
      isCancelled = true;
    };
  }, [page, appliedSearch, reloadIndex]);

  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const changeSearch = useCallback((value: string) => setSearchInput(value), []);
  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  const state = useMemo(
    () => ({
      accounts,
      totalCount,
      page,
      pageSize: PAGE_SIZE,
      search: searchInput,
      appliedSearch,
      isLoading,
      error,
    }),
    [accounts, totalCount, page, searchInput, appliedSearch, isLoading, error],
  );

  const actions = useMemo(
    () => ({ reload, changeSearch, goToPage }),
    [reload, changeSearch, goToPage],
  );

  return { state, actions };
}
