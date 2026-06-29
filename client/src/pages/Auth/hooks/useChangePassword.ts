import { useCallback, useMemo, useState, type FormEvent } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import { validateChangePassword, type ChangePasswordFields } from "@/lib/validation/auth";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";

type UseChangePasswordOptions = {
  onSuccess?: () => void;
  successMessage?: string;
};

export function useChangePassword(options: UseChangePasswordOptions = {}) {
  const { onSuccess, successMessage } = options;
  const setAuthenticatedUser = useUserStore((state) => state.setAuthenticatedUser);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors<ChangePasswordFields>>({});
  const [formError, setFormError] = useState<string | undefined>(undefined);
  const [notice, setNotice] = useState<string | undefined>(undefined);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      setFormError(undefined);
      setNotice(undefined);

      const validationErrors = validateChangePassword({ currentPassword, newPassword, confirmPassword });
      setErrors(validationErrors);
      if (hasErrors(validationErrors)) {
        return;
      }

      setIsSubmitting(true);
      try {
        const authResponse = await authService.changePassword({ currentPassword, newPassword });
        // Новата сесия идва с обновен потребител (mustChangePassword вече е false).
        setAuthenticatedUser(authResponse.user);
        if (successMessage) {
          setNotice(successMessage);
        }
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
        onSuccess?.();
      } catch (error) {
        setFormError(getCommonModelErrorMessage(error, "Паролата не може да бъде сменена."));
      } finally {
        setIsSubmitting(false);
      }
    },
    [currentPassword, newPassword, confirmPassword, setAuthenticatedUser, onSuccess, successMessage],
  );

  const state = useMemo(
    () => ({ currentPassword, newPassword, confirmPassword, errors, formError, notice, isSubmitting }),
    [currentPassword, newPassword, confirmPassword, errors, formError, notice, isSubmitting],
  );
  const actions = useMemo(
    () => ({ setCurrentPassword, setNewPassword, setConfirmPassword, submit }),
    [submit],
  );

  return { state, actions };
}
