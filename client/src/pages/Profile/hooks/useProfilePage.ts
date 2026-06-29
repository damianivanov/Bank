import { useCallback, useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";

export function useProfilePage() {
  const { user, setAuthenticatedUser } = useUserStore();
  const [firstName, setFirstName] = useState(user.firstName ?? "");
  const [lastName, setLastName] = useState(user.lastName ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Свързаните с лице акаунти черпят името от данните на лицето (управлявани от банката) —
  // то е само за четене тук и не може да се променя през профила.
  const isNameManaged = user.personId != null;

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (isNameManaged) {
        return;
      }
      setIsSubmitting(true);

      try {
        const updatedUser = await authService.updateProfile({ firstName, lastName });
        setAuthenticatedUser(updatedUser);
        toast.success("Профилът е обновен");
      } catch (error) {
        toast.error(getCommonModelErrorMessage(error, "Профилът не може да бъде обновен"));
      } finally {
        setIsSubmitting(false);
      }
    },
    [firstName, lastName, isNameManaged, setAuthenticatedUser],
  );

  const state = useMemo(
    () => ({ firstName, lastName, email: user.email, isSubmitting, isNameManaged }),
    [firstName, lastName, user.email, isSubmitting, isNameManaged],
  );
  const actions = useMemo(() => ({ setFirstName, setLastName, submit }), [submit]);

  return { state, actions };
}
