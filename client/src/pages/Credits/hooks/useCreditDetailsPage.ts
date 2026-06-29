import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { creditService } from "@/services/creditService";
import { type CreditDetails } from "@/types";

export function useCreditDetailsPage() {
  const { creditId } = useParams();
  const navigate = useNavigate();
  const parsedCreditId = Number(creditId);

  const [credit, setCredit] = useState<CreditDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const loadCredit = useCallback(async () => {
    if (!Number.isFinite(parsedCreditId) || parsedCreditId <= 0) {
      toast.error("Невалиден идентификатор на кредит");
      navigate("/credits", { replace: true });
      return;
    }

    setIsLoading(true);
    try {
      const creditDetails = await creditService.getCredit(parsedCreditId);
      setCredit(creditDetails);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Кредитът не можа да бъде зареден"));
      navigate("/credits", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedCreditId]);

  useEffect(() => {
    void loadCredit();
  }, [loadCredit]);

  const state = useMemo(() => ({ credit, isLoading }), [credit, isLoading]);

  return { state };
}
