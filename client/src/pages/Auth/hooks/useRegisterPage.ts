import { useCallback, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import { validateRegister, type RegisterFields } from "@/lib/validation/auth";
import { authService } from "@/services/authService";

export function useRegisterPage() {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors<RegisterFields>>({});
  const [formError, setFormError] = useState<string | undefined>(undefined);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      setFormError(undefined);
      const validationErrors = validateRegister({ firstName, lastName, email, password });
      setErrors(validationErrors);
      if (hasErrors(validationErrors)) {
        return;
      }

      setIsSubmitting(true);

      try {
        // Регистрацията не открива сесия — насочваме към login за изричен вход.
        await authService.register({ firstName, lastName, email, password });
        navigate("/login", { replace: true, state: { justRegistered: true, email } });
      } catch (error) {
        setFormError(getCommonModelErrorMessage(error, "Профилът не може да бъде създаден."));
      } finally {
        setIsSubmitting(false);
      }
    },
    [email, firstName, lastName, navigate, password],
  );

  const state = useMemo(
    () => ({ firstName, lastName, email, password, errors, formError, isSubmitting }),
    [firstName, lastName, email, password, errors, formError, isSubmitting],
  );
  const actions = useMemo(
    () => ({ setFirstName, setLastName, setEmail, setPassword, submit }),
    [submit],
  );

  return { state, actions };
}
