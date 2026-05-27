import { useState, type ChangeEvent, type FormEvent } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";
import { TextInputField } from "@/shared/components";

export default function Register() {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { isAuthenticated, userLoaded } = useUserStore();
  const setAuthenticatedUser = useUserStore((state) => state.setAuthenticatedUser);
  const navigate = useNavigate();

  if (!userLoaded) {
    return null;
  }

  if (userLoaded && isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleFirstNameChange = (event: ChangeEvent<HTMLInputElement>) => {
    setFirstName(event.target.value);
  };

  const handleLastNameChange = (event: ChangeEvent<HTMLInputElement>) => {
    setLastName(event.target.value);
  };

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
      const authResponse = await authService.register({
        firstName,
        lastName,
        email,
        password,
      });
      setAuthenticatedUser(authResponse.user);
      navigate("/dashboard", { replace: true });
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not create the account"));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="mx-auto flex min-h-[calc(100dvh-6rem)] w-full max-w-md items-center px-4 py-10">
      <form onSubmit={handleSubmit} className="bank-panel w-full rounded-3xl p-6">
        <h1 className="text-2xl font-bold tracking-tight">Register</h1>
        <p className="mt-2 text-sm text-secondary">Create a user account for the banking system.</p>
        <div className="mt-6 space-y-4">
          <TextInputField label="First name" name="firstName" value={firstName} onChange={handleFirstNameChange} required />
          <TextInputField label="Last name" name="lastName" value={lastName} onChange={handleLastNameChange} required />
          <TextInputField label="Email" name="email" type="email" value={email} onChange={handleEmailChange} required />
          <TextInputField label="Password" name="password" type="password" value={password} onChange={handlePasswordChange} required />
        </div>
        <button type="submit" disabled={isSubmitting} className="bank-primary-btn mt-6 w-full rounded-xl px-4 py-3 text-sm font-semibold disabled:opacity-60">
          {isSubmitting ? "Creating..." : "Create account"}
        </button>
        <p className="mt-4 text-center text-sm text-secondary">
          Already registered? <Link className="bank-accent-link font-semibold" to="/login">Login</Link>
        </p>
      </form>
    </section>
  );
}

