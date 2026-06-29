import { Check } from "lucide-react";
import { PageBody, TextInputField } from "@/shared/components";
import ChangePasswordForm from "./components/ChangePasswordForm";
import { useProfilePage } from "./hooks/useProfilePage";

export default function Profile() {
  const { state, actions } = useProfilePage();

  return (
    <PageBody>
      <h1 className="text-3xl font-bold tracking-tight">Профил</h1>
      <form onSubmit={actions.submit} className="bank-panel mt-6 rounded-2xl p-5">
        <div className="space-y-4">
          <TextInputField
            label="Име"
            name="firstName"
            value={state.firstName}
            onChange={(event) => actions.setFirstName(event.target.value)}
            readOnly={state.isNameManaged}
            required={!state.isNameManaged}
          />
          <TextInputField
            label="Фамилия"
            name="lastName"
            value={state.lastName}
            onChange={(event) => actions.setLastName(event.target.value)}
            readOnly={state.isNameManaged}
            required={!state.isNameManaged}
          />
          <TextInputField label="Имейл" value={state.email} readOnly />
        </div>
        {state.isNameManaged ? (
          <p className="mt-4 text-sm text-secondary">
            Името се управлява от банката и не може да се променя оттук.
          </p>
        ) : (
          <button
            type="submit"
            disabled={state.isSubmitting}
            className="bank-primary-btn mt-5 bank-btn disabled:opacity-60"
          >
            <Check className="h-4 w-4" />
            {state.isSubmitting ? "Запазване..." : "Запази промените"}
          </button>
        )}
      </form>

      <ChangePasswordForm />
    </PageBody>
  );
}
