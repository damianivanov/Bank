import { useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { myBankingService } from "@/services/myBankingService";
import type { DepositRequest } from "@/types";

export function useMyDepositRequests(refreshSignal: number) {
  const [requests, setRequests] = useState<DepositRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadRequests() {
      setIsLoading(true);
      setError(null);
      try {
        const data = await myBankingService.getMyDepositRequests();
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

    void loadRequests();

    return () => {
      isCancelled = true;
    };
  }, [refreshSignal]);

  const state = useMemo(() => ({ requests, isLoading, error }), [requests, isLoading, error]);

  return { state };
}
