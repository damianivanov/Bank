import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { savedCalculationService } from "@/services/savedCalculationService";
import { useUserStore } from "@/stores/userStore";
import type { SaveCalculationRequest, SavedCalculationDetailsModel, SavedCalculationModel } from "@/types";

export function useSavedCalculations() {
  const isAuthenticated = useUserStore((s) => s.isAuthenticated);

  const [items, setItems] = useState<SavedCalculationModel[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    try {
      const list = await savedCalculationService.list();
      setItems(list);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Запазените изчисления не бяха заредени"));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!isAuthenticated) {
      setItems([]);
      return;
    }
    void refresh();
  }, [isAuthenticated, refresh]);

  const save = useCallback(
    async (payload: SaveCalculationRequest): Promise<boolean> => {
      setIsSaving(true);
      try {
        await savedCalculationService.save(payload);
        toast.success("Изчислението е запазено");
        await refresh();
        return true;
      } catch (error) {
        toast.error(getCommonModelErrorMessage(error, "Изчислението не бе запазено"));
        return false;
      } finally {
        setIsSaving(false);
      }
    },
    [refresh],
  );

  const update = useCallback(
    async (id: number, payload: SaveCalculationRequest): Promise<boolean> => {
      setIsSaving(true);
      try {
        await savedCalculationService.update(id, payload);
        toast.success("Изчислението е обновено");
        await refresh();
        return true;
      } catch (error) {
        toast.error(getCommonModelErrorMessage(error, "Изчислението не бе обновено"));
        return false;
      } finally {
        setIsSaving(false);
      }
    },
    [refresh],
  );

  const load = useCallback(async (id: number): Promise<SavedCalculationDetailsModel | null> => {
    setLoadingId(id);
    try {
      return await savedCalculationService.get(id);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Запазеното изчисление не бе заредено"));
      return null;
    } finally {
      setLoadingId(null);
    }
  }, []);

  const remove = useCallback(
    async (id: number) => {
      setDeletingId(id);
      try {
        await savedCalculationService.remove(id);
        toast.success("Изчислението е изтрито");
        await refresh();
      } catch (error) {
        toast.error(getCommonModelErrorMessage(error, "Запазеното изчисление не бе изтрито"));
      } finally {
        setDeletingId(null);
      }
    },
    [refresh],
  );

  const state = useMemo(
    () => ({ items, isLoading, isSaving, loadingId, deletingId }),
    [items, isLoading, isSaving, loadingId, deletingId],
  );

  const actions = useMemo(
    () => ({ refresh, save, update, load, remove }),
    [refresh, save, update, load, remove],
  );

  return { state, actions };
}
