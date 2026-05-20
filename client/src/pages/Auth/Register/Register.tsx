import { useState, type ChangeEvent, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";
import { TextInputField } from "@/shared/components";

type RegisterForm = {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
};

const initialForm: RegisterForm = {
  email: "",
  password: "",
  firstName: "",
  lastName: "",
};

export default function Register() {
  const [form, setForm] = useState<RegisterForm>(initialForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const setAuthenticatedUser = useUserStore((state) => state.setAuthenticatedUser);
  const navigate = useNavigate();

  const handleFieldChange = (event: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const response = await authService.register(form);
      const result = response.data;

      if (!result.success || !result.data) {
        toast.error(result.error || "Registration failed");
        return;
      }

      setAuthenticatedUser(result.data.user);
      navigate("/dashboard", { replace: true });
    } catch {
      toast.error("Could not create the account");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="mx-auto flex min-h-[calc(100dvh-6rem)] max-w-md items-center px-4 py-10">
      <form onSubmit={handleSubmit} className="bank-panel w-full rounded-3xl p-6">
        <h1 className="text-2xl font-bold tracking-tight">Register</h1>
        <p className="mt-2 text-sm text-secondary">Create a user account for the banking system.</p>
        <div className="mt-6 grid gap-4 sm:grid-cols-2">
          <TextInputField label="First name" name="firstName" value={form.firstName} onChange={handleFieldChange} required />
          <TextInputField label="Last name" name="lastName" value={form.lastName} onChange={handleFieldChange} required />
          <TextInputField label="Email" name="email" type="email" value={form.email} onChange={handleFieldChange} required className="sm:col-span-2" />
          <TextInputField label="Password" name="password" type="password" value={form.password} onChange={handleFieldChange} required className="sm:col-span-2" />
        </div>
        <button type="submit" disabled={isSubmitting} className="bank-primary-btn mt-6 w-full rounded-xl px-4 py-3 text-sm font-semibold disabled:opacity-60">
          {isSubmitting ? "Creating..." : "Create account"}
        </button>
        <p className="mt-4 text-center text-sm text-secondary">
          Already registered? <Link className="font-semibold text-emerald-700" to="/login">Login</Link>
        </p>
      </form>
    </section>
  );
}
