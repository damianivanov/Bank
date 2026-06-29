import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { userManagementService } from "@/services/userManagementService";
import type { RegisterCounterCustomerRequest } from "@/types";

type UseCounterUserCreateModalArgs = {
  onClose: () => void;
  onCreated: () => void;
};

export function useCounterUserCreateModal({ onClose, onCreated }: UseCounterUserCreateModalArgs) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const submit = useCallback(
    async (payload: RegisterCounterCustomerRequest) => {
      setIsSubmitting(true);
      try {
        await userManagementService.createCounterUser(payload);
        toast.success("Потребителят е създаден. Началната парола е ЕГН-то.");
        onCreated();
        onClose();
      } catch (error) {
        toast.error(getCommonModelErrorMessage(error, "Потребителят не може да бъде създаден"));
      } finally {
        setIsSubmitting(false);
      }
    },
    [onClose, onCreated],
  );

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }
    onClose();
  }, [isSubmitting, onClose]);

  const state = useMemo(() => ({ isSubmitting }), [isSubmitting]);
  const actions = useMemo(() => ({ submit, close }), [submit, close]);

  return { state, actions };
}
