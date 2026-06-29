import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { errorService } from "@/services/errorService";
import type { ApiError } from "@/types";

const SEARCH_DEBOUNCE_MS = 250;
const PAGE_SIZE = 20;

export function useErrorsPage() {
  const [errors, setErrors] = useState<ApiError[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [selectedError, setSelectedError] = useState<ApiError | null>(null);
  // Монотонен брояч на заявките: записваме данните само от последната заявка (последната печели),
  // за да не презапише изостанал отговор по-нов резултат при бърза смяна на търсене/период/страница.
  const requestSeq = useRef(0);

  // Дебоунс на търсенето: при писане изчакваме, прилагаме термина и връщаме на първа страница.
  useEffect(() => {
    const handle = window.setTimeout(() => {
      setAppliedSearch(searchTerm.trim());
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [searchTerm]);

  useEffect(() => {
    let isCancelled = false;
    const seq = ++requestSeq.current;
    setIsLoading(true);

    async function loadErrors() {
      try {
        const data = await errorService.getErrors(
          { page, pageSize: PAGE_SIZE, search: appliedSearch || undefined },
          fromDate || undefined,
          toDate || undefined,
        );
        if (seq === requestSeq.current) {
          setErrors(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (error) {
        if (seq === requestSeq.current) {
          setErrors([]);
          setTotalCount(0);
          toast.error(getCommonModelErrorMessage(error, "Грешките не можаха да бъдат заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadErrors();

    return () => {
      isCancelled = true;
    };
  }, [page, appliedSearch, fromDate, toDate]);

  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);
  const openError = useCallback((error: ApiError) => setSelectedError(error), []);
  const closeError = useCallback(() => setSelectedError(null), []);

  // Промяна на период връща на първа страница, за да не остане извън наличния диапазон.
  const changeFromDate = useCallback((value: string) => {
    setFromDate(value);
    setPage(1);
  }, []);
  const changeToDate = useCallback((value: string) => {
    setToDate(value);
    setPage(1);
  }, []);

  const state = useMemo(
    () => ({ isLoading, errors, totalCount, page, pageSize: PAGE_SIZE, searchTerm, fromDate, toDate, selectedError }),
    [isLoading, errors, totalCount, page, searchTerm, fromDate, toDate, selectedError],
  );
  const actions = useMemo(
    () => ({ setSearchTerm, changeFromDate, changeToDate, goToPage, openError, closeError }),
    [changeFromDate, changeToDate, goToPage, openError, closeError],
  );

  return { state, actions };
}
