import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowLeft } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import CustomerForm, { type CustomerFormValue } from "@/pages/Customers/CustomerForm";
import { userManagementService } from "@/services/userManagementService";
import { CustomerType, type UserAccess } from "@/types";
import { formatUserName } from "./userAccess.utils";

function buildInitialValue(user: UserAccess): CustomerFormValue {
  return {
    customerType: CustomerType.Individual,
    firstName: user.firstName?.trim() || undefined,
    lastName: user.lastName?.trim() || undefined,
  };
}

export default function UserCustomerCreatePage() {
  const { userId } = useParams();
  const navigate = useNavigate();
  const parsedUserId = Number(userId);

  const [user, setUser] = useState<UserAccess | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadUser = useCallback(async () => {
    if (!Number.isFinite(parsedUserId) || parsedUserId <= 0) {
      toast.error("Invalid user id");
      navigate("/users", { replace: true });
      return;
    }

    setIsLoading(true);

    try {
      const selectedUser = await userManagementService.getUserById(parsedUserId);
      if (selectedUser.customerId) {
        toast.error("This user already has a linked customer.");
        navigate(`/users/${selectedUser.id}`, { replace: true });
        return;
      }

      setUser(selectedUser);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load user details"));
      navigate("/users", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedUserId]);

  useEffect(() => {
    void loadUser();
  }, [loadUser]);

  const handleBackClick = () => {
    if (Number.isFinite(parsedUserId) && parsedUserId > 0) {
      navigate(`/users/${parsedUserId}`);
      return;
    }

    navigate("/users");
  };

  const handleSubmit = async (payload: CustomerFormValue) => {
    if (!user) {
      return;
    }

    setIsSubmitting(true);

    try {
      await userManagementService.createCustomerForUser(user.id, payload);
      toast.success("Customer created and connected to user");
      navigate(`/users/${user.id}`);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not create customer for user"));
    } finally {
      setIsSubmitting(false);
    }
  };

  const initialValue = useMemo(() => (user ? buildInitialValue(user) : null), [user]);
  const userDisplayName = useMemo(() => (user ? formatUserName(user) : ""), [user]);

  if (isLoading || !user || !initialValue) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading user...</p>
      </section>
    );
  }

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="mx-auto w-full max-w-4xl">
        <div className="flex flex-wrap items-start gap-3">
          <button
            type="button"
            onClick={handleBackClick}
            className="bank-secondary-btn inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold"
          >
            <ArrowLeft className="h-4 w-4" />
            Back
          </button>

          <div>
            <h1 className="text-3xl font-bold tracking-tight">Create customer</h1>
            <p className="mt-1 text-sm text-secondary">
              This customer will be connected to <span className="font-semibold text-foreground">{userDisplayName}</span>.
            </p>
          </div>
        </div>

        <CustomerForm
          key={user.id}
          initialValue={initialValue}
          submitLabel="Create customer"
          isSubmitting={isSubmitting}
          onSubmit={handleSubmit}
        />
      </div>
    </section>
  );
}
