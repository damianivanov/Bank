import { useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { myBankingService } from "@/services/myBankingService";
import type { DepositRequest } from "@/types";

export function useMyDepositRequests(refreshSignal: number, customerId: number | null) {
  const [requests, setRequests] = useState<DepositRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (customerId == null) {
      return;
    }

    let isCancelled = false;

    async function loadRequests(targetCustomerId: number) {
      setIsLoading(true);
      setError(null);
      try {
        const data = await myBankingService.getMyDepositRequests(targetCustomerId);
        if (!isCancelled) {
          setRequests(data);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setRequests([]);
          setError(getCommonModelErrorMessage(loadError, "Заявките ви за депозит не бяха заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadRequests(customerId);

    return () => {
      isCancelled = true;
    };
  }, [refreshSignal, customerId]);

  const state = useMemo(() => ({ requests, isLoading, error }), [requests, isLoading, error]);

  return { state };
}
