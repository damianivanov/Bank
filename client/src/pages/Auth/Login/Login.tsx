import { useState, type FormEvent, type ChangeEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";
import { TextInputField } from "@/shared/components";

type LoginForm = {
  email: string;
  password: string;
};

const initialForm: LoginForm = {
  email: "",
  password: "",
};

export default function Login() {
  const [form, setForm] = useState<LoginForm>(initialForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const setAuthenticatedUser = useUserStore((state) => state.setAuthenticatedUser);
  const navigate = useNavigate();
  const location = useLocation();

  const handleFieldChange = (event: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const response = await authService.login(form);
      const result = response.data;

      if (!result.success || !result.data) {
        toast.error(result.error || "Login failed");
        return;
      }

      setAuthenticatedUser(result.data.user);
      const redirectTo = (location.state as { from?: Location } | null)?.from?.pathname || "/dashboard";
      navigate(redirectTo, { replace: true });
    } catch {
      toast.error("Invalid email or password");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="mx-auto flex min-h-[calc(100dvh-6rem)] max-w-md items-center px-4 py-10">
      <form onSubmit={handleSubmit} className="bank-panel w-full rounded-3xl p-6">
        <h1 className="text-2xl font-bold tracking-tight">Login</h1>
        <div className="mt-6 space-y-4">
          <TextInputField label="Email" name="email" type="email" value={form.email} onChange={handleFieldChange} required />
          <TextInputField label="Password" name="password" type="password" value={form.password} onChange={handleFieldChange} required />
        </div>
        <button type="submit" disabled={isSubmitting} className="bank-primary-btn mt-6 w-full rounded-xl px-4 py-3 text-sm font-semibold disabled:opacity-60">
          {isSubmitting ? "Signing in..." : "Login"}
        </button>
        <p className="mt-4 text-center text-sm text-secondary">
          No account? <Link className="bank-accent-link font-semibold" to="/register">Register</Link>
        </p>
      </form>
    </section>
  );
}
