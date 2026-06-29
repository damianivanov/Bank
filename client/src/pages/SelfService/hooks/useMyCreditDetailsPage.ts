import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { myBankingService } from "@/services/myBankingService";
import { type CreditDetails } from "@/types";

export function useMyCreditDetailsPage() {
  const { creditId } = useParams();
  const navigate = useNavigate();
  const parsedCreditId = Number(creditId);

  const [credit, setCredit] = useState<CreditDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isPayModalOpen, setIsPayModalOpen] = useState(false);

  const loadCredit = useCallback(async () => {
    if (!Number.isFinite(parsedCreditId) || parsedCreditId <= 0) {
      toast.error("Невалиден идентификатор на кредит");
      navigate("/my-banking", { replace: true });
      return;
    }

    setIsLoading(true);
    try {
      const creditDetails = await myBankingService.getCredit(parsedCreditId);
      setCredit(creditDetails);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Вашият кредит не можа да бъде зареден"));
      navigate("/my-banking", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedCreditId]);

  useEffect(() => {
    void loadCredit();
  }, [loadCredit]);

  const openPayModal = useCallback(() => setIsPayModalOpen(true), []);
  const closePayModal = useCallback(() => setIsPayModalOpen(false), []);

  // Платимостта се изчислява от backend-а (настъпил падеж + dev разрешител), за да е единствен източник на истината.
  const canPayInstallment = credit?.canPayNextInstallment ?? false;

  const state = useMemo(
    () => ({ credit, isLoading, isPayModalOpen, canPayInstallment }),
    [credit, isLoading, isPayModalOpen, canPayInstallment],
  );
  const actions = useMemo(
    () => ({ openPayModal, closePayModal, reload: loadCredit }),
    [openPayModal, closePayModal, loadCredit],
  );

  return { state, actions };
}
