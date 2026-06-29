import { useCallback, useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { isAdmin } from "@/lib/access";
import { creditConditionService } from "@/services/creditConditionService";
import { useUserStore } from "@/stores/userStore";
import type { CreditTypeCondition } from "@/types";

export function useCreditConditionsPage() {
  const { user } = useUserStore();
  const canEdit = isAdmin(user);

  const [conditions, setConditions] = useState<CreditTypeCondition[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadIndex, setReloadIndex] = useState(0);
  const [editingCondition, setEditingCondition] = useState<CreditTypeCondition | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadConditions() {
      setIsLoading(true);
      setError(null);

      try {
        const data = await creditConditionService.getCreditConditions();
        if (!isCancelled) {
          setConditions(data);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setConditions([]);
          setError(getCommonModelErrorMessage(loadError, "Кредитните условия не можаха да бъдат заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadConditions();

    return () => {
      isCancelled = true;
    };
  }, [reloadIndex]);

  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const startEdit = useCallback((condition: CreditTypeCondition) => setEditingCondition(condition), []);
  const closeEdit = useCallback(() => setEditingCondition(null), []);

  const state = useMemo(
    () => ({ conditions, isLoading, error, canEdit, editingCondition }),
    [conditions, isLoading, error, canEdit, editingCondition],
  );
  const actions = useMemo(() => ({ reload, startEdit, closeEdit }), [reload, startEdit, closeEdit]);

  return { state, actions };
}
