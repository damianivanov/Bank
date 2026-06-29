import { Link } from "react-router-dom";
import { UserPlus } from "lucide-react";
import { FormError, TextInputField } from "@/shared/components";
import { AuthScreen } from "../components";
import { useRegisterPage } from "../hooks/useRegisterPage";

export default function Register() {
  const { state, actions } = useRegisterPage();

  return (
    <AuthScreen>
      <form onSubmit={actions.submit} className="bank-panel bank-rise w-full rounded-3xl p-6 sm:p-7">
        <h1 className="text-2xl font-bold tracking-tight">Регистрация</h1>
        <p className="mt-2 text-sm text-secondary">Създайте потребителски профил за банковата система.</p>
        <div className="mt-6 space-y-4">
          <TextInputField
            label="Име"
            name="firstName"
            value={state.firstName}
            onChange={(event) => actions.setFirstName(event.target.value)}
            error={state.errors.firstName}
          />
          <TextInputField
            label="Фамилия"
            name="lastName"
            value={state.lastName}
            onChange={(event) => actions.setLastName(event.target.value)}
            error={state.errors.lastName}
          />
          <TextInputField
            label="Имейл"
            name="email"
            type="email"
            value={state.email}
            onChange={(event) => actions.setEmail(event.target.value)}
            error={state.errors.email}
          />
          <TextInputField
            label="Парола"
            name="password"
            type="password"
            value={state.password}
            onChange={(event) => actions.setPassword(event.target.value)}
            error={state.errors.password}
          />
        </div>
        <FormError message={state.formError} />
        <button
          type="submit"
          disabled={state.isSubmitting}
          className="bank-primary-btn mt-6 inline-flex w-full items-center justify-center gap-2 rounded-xl px-4 py-3 text-sm font-semibold disabled:opacity-60"
        >
          <UserPlus className="h-4 w-4" />
          {state.isSubmitting ? "Създаване..." : "Създай профил"}
        </button>
        <p className="mt-4 text-center text-sm text-secondary">
          Вече имате профил?{" "}
          <Link className="bank-accent-link font-semibold" to="/login">
            Влез
          </Link>
        </p>
      </form>
    </AuthScreen>
  );
}
