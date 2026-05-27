import { useState, type FormEvent, type ChangeEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";
import { TextInputField } from "@/shared/components";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { isAuthenticated, userLoaded } = useUserStore();
  const setAuthenticatedUser = useUserStore((state) => state.setAuthenticatedUser);
  const navigate = useNavigate();
  const location = useLocation();

  if (!userLoaded) {
    return null;
  }

  if (userLoaded && isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleEmailChange = (event: ChangeEvent<HTMLInputElement>) => {
    setEmail(event.target.value);
  };

  const handlePasswordChange = (event: ChangeEvent<HTMLInputElement>) => {
    setPassword(event.target.value);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const authResponse = await authService.login({ email, password });
      setAuthenticatedUser(authResponse.user);
      const redirectTo = (location.state as { from?: Location } | null)?.from?.pathname || "/dashboard";
      navigate(redirectTo, { replace: true });
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Invalid email or password"));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="mx-auto flex min-h-[calc(100dvh-6rem)] w-full max-w-md items-center px-4 py-10">
      <form onSubmit={handleSubmit} className="bank-panel w-full rounded-3xl p-6">
        <h1 className="text-2xl font-bold tracking-tight">Login</h1>
        <div className="mt-6 space-y-4">
          <TextInputField label="Email" name="email" type="email" value={email} onChange={handleEmailChange} required />
          <TextInputField label="Password" name="password" type="password" value={password} onChange={handlePasswordChange} required />
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

