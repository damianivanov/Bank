import { useCallback, useMemo, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import { validateLogin, type LoginFields } from "@/lib/validation/auth";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";

type LoginRedirectState = {
  from?: Location;
  justRegistered?: boolean;
  email?: string;
};

export function useLoginPage() {
  const location = useLocation();
  const redirectState = location.state as LoginRedirectState | null;
  const [email, setEmail] = useState(redirectState?.email ?? "");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors<LoginFields>>({});
  const [formError, setFormError] = useState<string | undefined>(undefined);
  const [notice, setNotice] = useState<string | undefined>(
    redirectState?.justRegistered ? "Регистрацията е успешна. Влезте в профила си." : undefined,
  );
  const [isSubmitting, setIsSubmitting] = useState(false);
  const setAuthenticatedUser = useUserStore((state) => state.setAuthenticatedUser);
  const navigate = useNavigate();

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      setFormError(undefined);
      setNotice(undefined);
      const validationErrors = validateLogin({ email, password });
      setErrors(validationErrors);
      if (hasErrors(validationErrors)) {
        return;
      }

      setIsSubmitting(true);

      try {
        const authResponse = await authService.login({ email, password });
        setAuthenticatedUser(authResponse.user);
        if (authResponse.user.mustChangePassword) {
          navigate("/change-password", { replace: true });
          return;
        }
        const redirectTo = redirectState?.from?.pathname || "/dashboard";
        navigate(redirectTo, { replace: true });
      } catch (error) {
        setFormError(getCommonModelErrorMessage(error, "Невалиден имейл или парола."));
      } finally {
        setIsSubmitting(false);
      }
    },
    [email, redirectState, navigate, password, setAuthenticatedUser],
  );

  const state = useMemo(
    () => ({ email, password, errors, formError, notice, isSubmitting }),
    [email, password, errors, formError, notice, isSubmitting],
  );
  const actions = useMemo(() => ({ setEmail, setPassword, submit }), [submit]);

  return { state, actions };
}
