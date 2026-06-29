import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { depositApprovalsService } from "@/services/depositApprovalsService";
import { DepositRequestStatus, type DepositRequestQueue } from "@/types";

// undefined == "All" в дропдауна за филтриране.
type StatusFilter = DepositRequestStatus | undefined;

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

export function useDepositApprovalsPage() {
  const [requests, setRequests] = useState<DepositRequestQueue[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [statusFilter, setStatusFilterState] = useState<StatusFilter>(DepositRequestStatus.Pending);
  const [processingId, setProcessingId] = useState<number | null>(null);
  const [rejectTarget, setRejectTarget] = useState<DepositRequestQueue | null>(null);
  const [approveTarget, setApproveTarget] = useState<DepositRequestQueue | null>(null);
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

    async function loadRequests() {
      setIsLoading(true);
      try {
        const data = await depositApprovalsService.getDepositRequests(statusFilter, {
          page,
          pageSize: PAGE_SIZE,
          search: appliedSearch || undefined,
        });
        if (!isCancelled) {
          setRequests(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setRequests([]);
          setTotalCount(0);
          toast.error(getCommonModelErrorMessage(loadError, "Заявките за депозит не бяха заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadRequests();

    return () => {
      isCancelled = true;
    };
  }, [statusFilter, page, appliedSearch, reloadIndex]);

  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const changeSearch = useCallback((value: string) => setSearchInput(value), []);
  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  // Смяната на статус-филтъра свива резултатите, затова връщаме потребителя на първа страница.
  const setStatusFilter = useCallback((status: StatusFilter) => {
    setStatusFilterState(status);
    setPage(1);
  }, []);

  const openApprove = useCallback((request: DepositRequestQueue) => setApproveTarget(request), []);
  const closeApprove = useCallback(() => {
    if (processingId !== null) {
      return;
    }
    setApproveTarget(null);
  }, [processingId]);

  const confirmApprove = useCallback(async () => {
    if (!approveTarget) {
      return;
    }
    setProcessingId(approveTarget.id);
    try {
      const result = await depositApprovalsService.approve(approveTarget.id);
      toast.success(`Депозитът е одобрен. Ново салдо: ${result.newBalance.toFixed(2)}`);
      setApproveTarget(null);
      reload();
    } catch (approveError) {
      toast.error(getCommonModelErrorMessage(approveError, "Заявката не бе одобрена"));
    } finally {
      setProcessingId(null);
    }
  }, [approveTarget, reload]);

  const openReject = useCallback((request: DepositRequestQueue) => setRejectTarget(request), []);
  const closeReject = useCallback(() => {
    if (processingId !== null) {
      return;
    }
    setRejectTarget(null);
  }, [processingId]);

  const confirmReject = useCallback(
    async (note: string) => {
      if (!rejectTarget) {
        return;
      }
      setProcessingId(rejectTarget.id);
      try {
        await depositApprovalsService.reject(rejectTarget.id, { note: note.trim() || undefined });
        toast.success("Заявката за депозит е отхвърлена");
        setRejectTarget(null);
        reload();
      } catch (rejectError) {
        toast.error(getCommonModelErrorMessage(rejectError, "Заявката не бе отхвърлена"));
      } finally {
        setProcessingId(null);
      }
    },
    [rejectTarget, reload],
  );

  const state = useMemo(
    () => ({
      requests,
      totalCount,
      page,
      pageSize: PAGE_SIZE,
      search: searchInput,
      appliedSearch,
      isLoading,
      statusFilter,
      processingId,
      rejectTarget,
      approveTarget,
    }),
    [
      requests,
      totalCount,
      page,
      searchInput,
      appliedSearch,
      isLoading,
      statusFilter,
      processingId,
      rejectTarget,
      approveTarget,
    ],
  );
  const actions = useMemo(
    () => ({
      setStatusFilter,
      changeSearch,
      goToPage,
      reload,
      openApprove,
      closeApprove,
      confirmApprove,
      openReject,
      closeReject,
      confirmReject,
    }),
    [
      setStatusFilter,
      changeSearch,
      goToPage,
      reload,
      openApprove,
      closeApprove,
      confirmApprove,
      openReject,
      closeReject,
      confirmReject,
    ],
  );

  return { state, actions };
}
