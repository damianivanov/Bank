import { Link } from "react-router-dom";
import { LogIn } from "lucide-react";
import { FormError, FormNotice, TextInputField } from "@/shared/components";
import { AuthScreen } from "../components";
import { useLoginPage } from "../hooks/useLoginPage";

export default function Login() {
  const { state, actions } = useLoginPage();

  return (
    <AuthScreen>
      <form onSubmit={actions.submit} className="bank-panel bank-rise w-full rounded-3xl p-6 sm:p-7">
        <h1 className="text-2xl font-bold tracking-tight">Вход</h1>
        <p className="mt-2 text-sm text-secondary">Добре дошли отново. Влезте в профила си.</p>
        <FormNotice message={state.notice} />
        <div className="mt-6 space-y-4">
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
          <LogIn className="h-4 w-4" />
          {state.isSubmitting ? "Влизане..." : "Влез"}
        </button>
        <p className="mt-4 text-center text-sm text-secondary">
          Нямате профил?{" "}
          <Link className="bank-accent-link font-semibold" to="/register">
            Регистрация
          </Link>
        </p>
      </form>
    </AuthScreen>
  );
}
