import { useEffect, useMemo, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import {
  formatRole,
  formatUserName,
} from "./userAccess.utils";
import UserRoleBadges from "./UserRoleBadges";
import { userManagementService } from "@/services/userManagementService";
import { EntityGrid } from "@/shared/components";
import type { StaffUserGrid } from "@/types";

const SEARCH_DEBOUNCE_MS = 250;

function renderUserStatusBadge(isActive: boolean) {
  return (
    <span
      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
        isActive ? "bank-chip bank-chip-success" : "bank-chip bank-chip-warn"
      }`}
    >
      {isActive ? "Active" : "Inactive"}
    </span>
  );
}

function renderCustomerDisplay(customerDisplayName?: string) {
  if (customerDisplayName) {
    return customerDisplayName;
  }

  return (
    <span className="bank-chip bank-chip-warn rounded-full px-2 py-0.5 text-xs font-semibold">
      Not linked
    </span>
  );
}

export default function StaffUserManagementPage() {
  const navigate = useNavigate();

  const [users, setUsers] = useState<StaffUserGrid[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState("");

  const handleSearchTermChange = (event: ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
  };

  const createOpenUserDetailsHandler = (userId: number) => () => {
    navigate(`/users/${userId}`);
  };

  const createOpenCustomerDetailsHandler = (customerId: number) => () => {
    navigate(`/customers/${customerId}`);
  };

  const loadUsers = async () => {
    setIsLoading(true);

    try {
      const usersData = await userManagementService.getCustomerGridUsers();
      setUsers(usersData);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load users"));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchTerm]);

  const filteredUsers = useMemo(() => {
    const normalizedSearchTerm = debouncedSearchTerm.trim().toLowerCase();

    return users.filter((user) => {
      if (!normalizedSearchTerm) {
        return true;
      }

      const searchHaystack = [
        user.email,
        user.firstName,
        user.lastName,
        user.customerDisplayName,
        ...user.roles.map((role) => formatRole(role)),
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return searchHaystack.includes(normalizedSearchTerm);
    });
  }, [debouncedSearchTerm, users]);

  const summary = useMemo(() => {
    const linked = users.filter((user) => Boolean(user.customerId)).length;
    const missingCustomer = Math.max(users.length - linked, 0);
    const inactive = users.filter((user) => !user.isActive).length;

    return {
      total: users.length,
      linked,
      missingCustomer,
      inactive,
    };
  }, [users]);

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Customer User Management</h1>
        <p className="mt-1 text-sm text-secondary">
          Manage customer-linked users and open related user/customer profiles.
        </p>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-2">
        <span className="bank-chip rounded-full px-3 py-1 text-xs font-semibold">Users: {summary.total}</span>
        <span className="bank-chip bank-chip-success rounded-full px-3 py-1 text-xs font-semibold">
          Linked customers: {summary.linked}
        </span>
        <span className="bank-chip bank-chip-warn rounded-full px-3 py-1 text-xs font-semibold">
          Missing customer: {summary.missingCustomer}
        </span>
        <span className="bank-chip bank-chip-warn rounded-full px-3 py-1 text-xs font-semibold">
          Inactive: {summary.inactive}
        </span>
      </div>

      <div className="mt-4">
        <input
          type="search"
          value={searchTerm}
          onChange={handleSearchTermChange}
          placeholder="Search by email, name, customer, or role..."
          className="bank-input w-full rounded-full! px-3 py-2.5 text-sm lg:max-w-md"
        />
      </div>

      <div className="mt-6 md:hidden">
        {isLoading ? (
          <p className="text-sm text-secondary">Loading users...</p>
        ) : filteredUsers.length === 0 ? (
          <p className="text-sm text-secondary">No customer users match your search.</p>
        ) : (
          <div className="space-y-3">
            {filteredUsers.map((user) => {
              const openUserDetailsHandler = createOpenUserDetailsHandler(user.id);
              const openCustomerDetailsHandler = user.customerId
                ? createOpenCustomerDetailsHandler(user.customerId)
                : undefined;

              return (
                <article key={user.id} className="bank-panel rounded-2xl p-4">
                  <div className="flex items-start justify-between gap-3">
                    <button
                      type="button"
                      onClick={openUserDetailsHandler}
                      className="min-w-0 cursor-pointer text-left text-sm font-semibold text-foreground underline-offset-4 transition hover:underline"
                    >
                      {user.email}
                    </button>
                    <button
                      type="button"
                      onClick={openUserDetailsHandler}
                      className="bank-secondary-btn inline-flex shrink-0 items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold"
                    >
                      View details
                    </button>
                  </div>

                  <p className="mt-2 text-sm text-secondary">{formatUserName(user)}</p>

                  <div className="mt-3">
                    <UserRoleBadges user={user} />
                  </div>

                  <div className="mt-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Customer</p>
                    <p className="mt-1 text-sm">{renderCustomerDisplay(user.customerDisplayName)}</p>
                  </div>

                  <div className="mt-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Status</p>
                    <div className="mt-1">{renderUserStatusBadge(user.isActive)}</div>
                  </div>

                  <div className="mt-3">
                    {openCustomerDetailsHandler ? (
                      <button
                        type="button"
                        onClick={openCustomerDetailsHandler}
                        className="bank-secondary-btn inline-flex items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold"
                      >
                        View customer
                      </button>
                    ) : (
                      <span className="text-xs text-secondary">No linked customer profile.</span>
                    )}
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </div>

      <div className="mt-6 hidden md:block">
        {isLoading ? (
          <p className="text-sm text-secondary">Loading users...</p>
        ) : (
          <EntityGrid>
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
                <th className="px-4 py-3 text-left">Actions</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Customer</th>
                <th className="px-4 py-3">Roles</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => {
                const openUserDetailsHandler = createOpenUserDetailsHandler(user.id);
                const openCustomerDetailsHandler = user.customerId
                  ? createOpenCustomerDetailsHandler(user.customerId)
                  : undefined;

                return (
                  <tr key={user.id} className="border-b border-slate-100 text-sm last:border-b-0">
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap justify-start gap-2">
                        <button
                          type="button"
                          onClick={openUserDetailsHandler}
                          className="bank-secondary-btn inline-flex items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold"
                        >
                          View details
                        </button>
                        {openCustomerDetailsHandler ? (
                          <button
                            type="button"
                            onClick={openCustomerDetailsHandler}
                            className="bank-secondary-btn inline-flex items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold"
                          >
                            View customer
                          </button>
                        ) : null}
                      </div>
                    </td>
                    <td className="px-4 py-3 font-medium">
                      <button
                        type="button"
                        onClick={openUserDetailsHandler}
                        className="cursor-pointer text-left text-foreground underline-offset-4 transition hover:underline"
                      >
                        {user.email}
                      </button>
                    </td>
                    <td className="px-4 py-3">{formatUserName(user)}</td>
                    <td className="px-4 py-3">{renderCustomerDisplay(user.customerDisplayName)}</td>
                    <td className="px-4 py-3">
                      <UserRoleBadges user={user} />
                    </td>
                    <td className="px-4 py-3">{renderUserStatusBadge(user.isActive)}</td>
                  </tr>
                );
              })}
              {filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-secondary">
                    No customer users match your search.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </EntityGrid>
        )}
      </div>
    </section>
  );
}
