import { KeyRound } from "lucide-react";
import { FormError, FormNotice, TextInputField } from "@/shared/components";
import { useChangePassword } from "@/pages/Auth/hooks/useChangePassword";

export default function ChangePasswordForm() {
  const { state, actions } = useChangePassword({ successMessage: "Паролата е сменена." });

  return (
    <form onSubmit={actions.submit} className="bank-panel mt-6 rounded-2xl p-5">
      <h2 className="text-lg font-bold">Смяна на парола</h2>
      <FormNotice message={state.notice} />
      <div className="mt-4 space-y-4">
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
  );
}
