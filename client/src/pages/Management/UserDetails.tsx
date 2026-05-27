import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowLeft } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import AccountCreateModal from "@/pages/Accounts/AccountCreateModal";
import CreditCreateModal from "@/pages/Credits/CreditCreateModal";
import { userManagementService } from "@/services/userManagementService";
import { formatRole, formatUserName, getRoleBadgeClassName } from "./userAccess.utils";
import { type UserAccess } from "@/types";

export default function UserDetailsPage() {
  const { userId } = useParams();
  const navigate = useNavigate();
  const parsedUserId = Number(userId);

  const [user, setUser] = useState<UserAccess | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreateAccountModalOpen, setIsCreateAccountModalOpen] = useState(false);
  const [isCreateCreditModalOpen, setIsCreateCreditModalOpen] = useState(false);

  const loadUser = useCallback(async () => {
    if (!Number.isFinite(parsedUserId) || parsedUserId <= 0) {
      toast.error("Invalid user id");
      navigate("/users", { replace: true });
      return;
    }

    setIsLoading(true);

    try {
      const selectedUser = await userManagementService.getUserById(parsedUserId);
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
    navigate("/users");
  };

  const handleCreateCustomerClick = () => {
    navigate(`/users/${parsedUserId}/customer`);
  };

  const handleOpenCreateAccountModal = () => {
    setIsCreateAccountModalOpen(true);
  };

  const handleCloseCreateAccountModal = () => {
    setIsCreateAccountModalOpen(false);
  };

  const handleAccountCreated = (accountId: number) => {
    navigate(`/accounts/${accountId}`);
  };

  const handleOpenCreateCreditModal = () => {
    setIsCreateCreditModalOpen(true);
  };

  const handleCloseCreateCreditModal = () => {
    setIsCreateCreditModalOpen(false);
  };

  const handleCreditCreated = (creditId: number) => {
    navigate(`/credits/${creditId}`);
  };

  const userDisplayName = useMemo(() => {
    if (!user) {
      return "";
    }

    return formatUserName(user);
  }, [user]);

  if (isLoading || !user) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading user details...</p>
      </section>
    );
  }

  const hasCustomer = Boolean(user.customerId);

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="mx-auto w-full max-w-4xl space-y-4">
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
            <h1 className="text-3xl font-bold tracking-tight">{userDisplayName}</h1>
            <p className="mt-1 text-sm text-secondary">{user.email}</p>
          </div>
        </div>

        <section className="bank-panel rounded-2xl p-4 md:p-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">User id</p>
              <p className="mt-2 text-sm font-semibold">{user.id}</p>
            </div>
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Status</p>
              <span
                className={`mt-2 inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
                  user.isActive ? "bank-chip bank-chip-success" : "bank-chip bank-chip-warn"
                }`}
              >
                {user.isActive ? "Active" : "Inactive"}
              </span>
            </div>
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Linked customer</p>
              <p className="mt-2 text-sm font-semibold">{user.customerDisplayName || "Not linked"}</p>
            </div>
            <div className="sm:col-span-2">
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Roles</p>
              <div className="mt-2 flex flex-wrap gap-1.5">
                {user.roles.map((role) => (
                  <span
                    key={`${user.id}-${role}`}
                    className={`bank-chip ${getRoleBadgeClassName(role)} rounded-full px-2 py-0.5 text-xs font-semibold`}
                  >
                    {formatRole(role)}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </section>

        {hasCustomer ? (
          <section className="bank-panel rounded-2xl p-4 md:p-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-xl font-bold tracking-tight">Actions</h2>
              <div className="flex flex-wrap gap-2">
                <Link
                  to={`/customers/${user.customerId}`}
                  className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold"
                >
                  View customer
                </Link>
                <button
                  type="button"
                  onClick={handleOpenCreateAccountModal}
                  className="bank-primary-btn rounded-lg px-3 py-1.5 text-xs font-semibold"
                >
                  Create account
                </button>
                <button
                  type="button"
                  onClick={handleOpenCreateCreditModal}
                  className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold"
                >
                  Grant credit
                </button>
              </div>
            </div>
          </section>
        ) : (
          <section className="bank-panel rounded-2xl p-4 md:p-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-xl font-bold tracking-tight">Actions</h2>
                <p className="mt-2 text-sm text-secondary">No customer is linked to this user yet.</p>
              </div>
              <button
                type="button"
                onClick={handleCreateCustomerClick}
                className="bank-primary-btn rounded-lg px-3 py-1.5 text-xs font-semibold"
              >
                Create customer
              </button>
            </div>
          </section>
        )}
      </div>

      {user.customerId && user.customerDisplayName ? (
        <AccountCreateModal
          isOpen={isCreateAccountModalOpen}
          customerId={user.customerId}
          customerDisplayName={user.customerDisplayName}
          onClose={handleCloseCreateAccountModal}
          onCreated={handleAccountCreated}
        />
      ) : null}

      {user.customerId && user.customerDisplayName ? (
        <CreditCreateModal
          isOpen={isCreateCreditModalOpen}
          customerId={user.customerId}
          customerDisplayName={user.customerDisplayName}
          onClose={handleCloseCreateCreditModal}
          onCreated={handleCreditCreated}
        />
      ) : null}
    </section>
  );
}
