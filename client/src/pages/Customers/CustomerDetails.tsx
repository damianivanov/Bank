import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { isAdmin } from "@/lib/access";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { customerService } from "@/services/customerService";
import { useUserStore } from "@/stores/userStore";
import {
  AccountStatusBadge,
  CreditStatusBadge,
  CustomerTypeBadge,
  EntityGrid,
  VipBadge,
} from "@/shared/components";
import { type CustomerDetails, CustomerType } from "@/types";
import CustomerUpsertModal from "./CustomerUpsertModal";

export default function CustomerDetailsPage() {
  const { customerId } = useParams();
  const navigate = useNavigate();
  const user = useUserStore((state) => state.user);
  const canManageVip = isAdmin(user);
  const parsedCustomerId = Number(customerId);

  const [customer, setCustomer] = useState<CustomerDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUpdatingVip, setIsUpdatingVip] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const loadCustomer = useCallback(async () => {
    if (!Number.isFinite(parsedCustomerId) || parsedCustomerId <= 0) {
      toast.error("Invalid customer id");
      navigate("/customers", { replace: true });
      return;
    }

    setIsLoading(true);

    try {
      const customerDetails = await customerService.getCustomer(parsedCustomerId);
      setCustomer(customerDetails);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load customer"));
      navigate("/customers", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedCustomerId]);

  useEffect(() => {
    void loadCustomer();
  }, [loadCustomer]);

  const handleVipToggle = async () => {
    if (!customer || !canManageVip) {
      return;
    }

    setIsUpdatingVip(true);
    try {
      const updatedCustomer = await customerService.updateCustomerVip(customer.id, { isVip: !customer.isVip });
      setCustomer(updatedCustomer);
      toast.success("VIP status updated");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not update VIP status"));
    } finally {
      setIsUpdatingVip(false);
    }
  };

  const handleEditModalOpen = () => {
    setIsEditModalOpen(true);
  };

  const handleEditModalClose = () => {
    setIsEditModalOpen(false);
  };

  const handleCustomerSaved = () => {
    void loadCustomer();
  };

  const customerName = useMemo(() => {
    if (!customer) {
      return "";
    }

    return customer.customerType === CustomerType.Individual
      ? `${customer.firstName ?? ""} ${customer.lastName ?? ""}`.trim()
      : customer.companyName ?? "";
  }, [customer]);

  if (isLoading || !customer) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading customer...</p>
      </section>
    );
  }

  const renderAccounts = () => {
    if (customer.accounts.length === 0) {
      return <p className="text-sm text-secondary">No accounts for this customer.</p>;
    }

    return (
      <EntityGrid>
        <thead>
          <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
            <th className="px-4 py-3">IBAN</th>
            <th className="px-4 py-3">Balance</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Opened</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody>
          {customer.accounts.map((account) => (
            <tr key={account.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3 font-mono text-xs">{account.iban}</td>
              <td className="px-4 py-3">{formatCurrency(account.balance)}</td>
              <td className="px-4 py-3">
                <AccountStatusBadge status={account.status} />
              </td>
              <td className="px-4 py-3">{formatDate(account.openedAtUtc)}</td>
              <td className="px-4 py-3 text-right">
                <Link to={`/accounts/${account.id}`} className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
                  Open
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </EntityGrid>
    );
  };

  const renderCredits = () => {
    if (customer.credits.length === 0) {
      return <p className="text-sm text-secondary">No credits for this customer.</p>;
    }

    return (
      <EntityGrid>
        <thead>
          <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Amount</th>
            <th className="px-4 py-3">Term</th>
            <th className="px-4 py-3">Rate</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody>
          {customer.credits.map((credit) => (
            <tr key={credit.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3">{credit.creditType === 1 ? "Consumer" : "Mortgage"}</td>
              <td className="px-4 py-3">{formatCurrency(credit.grantedAmount)}</td>
              <td className="px-4 py-3">{credit.termMonths} months</td>
              <td className="px-4 py-3">{formatPercent(credit.appliedAnnualInterestRate)}</td>
              <td className="px-4 py-3">
                <CreditStatusBadge status={credit.status} />
              </td>
              <td className="px-4 py-3 text-right">
                <Link to={`/credits/${credit.id}`} className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
                  Open
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </EntityGrid>
    );
  };

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{customerName}</h1>
          <p className="mt-1 text-sm text-secondary">Customer details and related accounts and credits.</p>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleEditModalOpen}
            className="bank-secondary-btn rounded-xl px-4 py-2 text-sm font-semibold"
          >
            Edit
          </button>
          {canManageVip ? (
            <button
              type="button"
              onClick={handleVipToggle}
              disabled={isUpdatingVip}
              className="bank-primary-btn rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
            >
              {isUpdatingVip ? "Updating..." : customer.isVip ? "Remove VIP" : "Make VIP"}
            </button>
          ) : null}
        </div>
      </div>

      <section className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4 md:grid-cols-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Type</p>
            <div className="mt-2">
              <CustomerTypeBadge customerType={customer.customerType} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">VIP</p>
            <div className="mt-2">
              <VipBadge isVip={customer.isVip} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Identifier</p>
            <p className="mt-2 text-sm font-semibold">
              {customer.customerType === CustomerType.Individual ? customer.personalIdentifier : customer.companyIdentifier}
            </p>
          </div>
        </div>

        {customer.customerType === CustomerType.Company ? (
          <p className="mt-4 text-sm text-secondary">Representative: {customer.representativeName}</p>
        ) : null}
      </section>

      <section className="mt-6">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-xl font-bold tracking-tight">Accounts</h2>
          <Link to="/accounts/new" className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
            Open account
          </Link>
        </div>
        {renderAccounts()}
      </section>

      <section className="mt-6">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-xl font-bold tracking-tight">Credits</h2>
          <Link to="/credits/new" className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
            Grant credit
          </Link>
        </div>
        {renderCredits()}
      </section>

      <CustomerUpsertModal
        isOpen={isEditModalOpen}
        mode="edit"
        customerId={customer.id}
        onClose={handleEditModalClose}
        onSaved={handleCustomerSaved}
      />
    </section>
  );
}

