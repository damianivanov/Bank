import { useCallback, useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { creditService } from "@/services/creditService";
import type { Credit } from "@/types";

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

export function useCreditsListPage() {
  const [credits, setCredits] = useState<Credit[]>([]);
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

    async function loadCredits() {
      setIsLoading(true);
      setError(null);

      try {
        const data = await creditService.getCredits({
          page,
          pageSize: PAGE_SIZE,
          search: appliedSearch || undefined,
        });
        if (!isCancelled) {
          setCredits(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setCredits([]);
          setTotalCount(0);
          setError(getCommonModelErrorMessage(loadError, "Кредитите не можаха да бъдат заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadCredits();

    return () => {
      isCancelled = true;
    };
  }, [page, appliedSearch, reloadIndex]);

  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const changeSearch = useCallback((value: string) => setSearchInput(value), []);
  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  const state = useMemo(
    () => ({
      credits,
      totalCount,
      page,
      pageSize: PAGE_SIZE,
      search: searchInput,
      appliedSearch,
      isLoading,
      error,
    }),
    [credits, totalCount, page, searchInput, appliedSearch, isLoading, error],
  );

  const actions = useMemo(
    () => ({ reload, changeSearch, goToPage }),
    [reload, changeSearch, goToPage],
  );

  return { state, actions };
}
