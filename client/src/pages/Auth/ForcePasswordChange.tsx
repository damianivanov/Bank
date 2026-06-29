import { useNavigate } from "react-router-dom";
import { KeyRound } from "lucide-react";
import { FormError, FormNotice, TextInputField } from "@/shared/components";
import { AuthScreen } from "./components";
import { useChangePassword } from "./hooks/useChangePassword";

export default function ForcePasswordChange() {
  const navigate = useNavigate();
  const { state, actions } = useChangePassword({
    onSuccess: () => navigate("/dashboard", { replace: true }),
  });

  return (
    <AuthScreen>
      <form onSubmit={actions.submit} className="bank-panel bank-rise w-full rounded-3xl p-6 sm:p-7">
        <h1 className="text-2xl font-bold tracking-tight">Смяна на паролата</h1>
        <p className="mt-2 text-sm text-secondary">
          За сигурност трябва да смените началната си парола, преди да продължите.
        </p>
        <FormNotice message={state.notice} />
        <div className="mt-5 space-y-4">
          <TextInputField
            label="Текуща парола"
            type="password"
            name="currentPassword"
            value={state.currentPassword}
            onChange={(event) => actions.setCurrentPassword(event.target.value)}
            error={state.errors.currentPassword}
          />
          <TextInputField
            label="Нова парола"
            type="password"
            name="newPassword"
            value={state.newPassword}
            onChange={(event) => actions.setNewPassword(event.target.value)}
            error={state.errors.newPassword}
          />
          <TextInputField
            label="Потвърдете новата парола"
            type="password"
            name="confirmPassword"
            value={state.confirmPassword}
            onChange={(event) => actions.setConfirmPassword(event.target.value)}
            error={state.errors.confirmPassword}
          />
        </div>
        <FormError message={state.formError} />
        <button type="submit" disabled={state.isSubmitting} className="bank-primary-btn mt-5 bank-btn disabled:opacity-60">
          <KeyRound className="h-4 w-4" />
          {state.isSubmitting ? "Запазване..." : "Смени паролата"}
        </button>
      </form>
    </AuthScreen>
  );
}
