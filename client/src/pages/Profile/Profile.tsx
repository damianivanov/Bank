import { useState, type ChangeEvent, type FormEvent } from "react";
import { toast } from "sonner";
import { authService } from "@/services/authService";
import { useUserStore } from "@/stores/userStore";
import { TextInputField } from "@/shared/components";

export default function Profile() {
  const { user, setAuthenticatedUser } = useUserStore();
  const [firstName, setFirstName] = useState(user.firstName ?? "");
  const [lastName, setLastName] = useState(user.lastName ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleFirstNameChange = (event: ChangeEvent<HTMLInputElement>) => {
    setFirstName(event.target.value);
  };

  const handleLastNameChange = (event: ChangeEvent<HTMLInputElement>) => {
    setLastName(event.target.value);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const response = await authService.updateProfile({ firstName, lastName });
      if (response.data.success && response.data.data) {
        setAuthenticatedUser(response.data.data);
        toast.success("Profile updated");
      }
    } catch {
      toast.error("Profile could not be updated");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="mx-auto w-full max-w-3xl px-4 py-6 md:px-8">
      <h1 className="text-3xl font-bold tracking-tight">Profile</h1>
      <form onSubmit={handleSubmit} className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <TextInputField label="First name" value={firstName} onChange={handleFirstNameChange} required />
          <TextInputField label="Last name" value={lastName} onChange={handleLastNameChange} required />
          <TextInputField label="Email" value={user.email} readOnly className="bg-slate-100 sm:col-span-2" />
        </div>
        <button type="submit" disabled={isSubmitting} className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60">
          {isSubmitting ? "Saving..." : "Save profile"}
        </button>
      </form>
    </section>
  );
}
