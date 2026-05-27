import { useState, type ChangeEvent, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
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
      const updatedUser = await authService.updateProfile({ firstName, lastName });
      setAuthenticatedUser(updatedUser);
      toast.success("Profile updated");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Profile could not be updated"));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <h1 className="text-3xl font-bold tracking-tight">Profile</h1>
      <form onSubmit={handleSubmit} className="bank-panel mt-6 rounded-2xl p-5">
        <div className="space-y-4">
          <TextInputField label="First name" name="firstName" value={firstName} onChange={handleFirstNameChange} required />
          <TextInputField label="Last name" name="lastName" value={lastName} onChange={handleLastNameChange} required />
          <TextInputField label="Email" value={user.email} readOnly className="bank-input-readonly" />
        </div>
        <button type="submit" disabled={isSubmitting} className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60">
          {isSubmitting ? "Saving..." : "Save profile"}
        </button>
      </form>
    </section>
  );
}

