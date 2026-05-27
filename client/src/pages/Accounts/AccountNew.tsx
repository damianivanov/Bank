import { useCallback, useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { accountService } from "@/services/accountService";
import { customerService } from "@/services/customerService";
import { Dropdown, TextInputField, VipBadge } from "@/shared/components";
import type { CreateBankAccountRequest, CustomerLookup } from "@/types";

type PreselectedCustomerState = {
  preselectedCustomerId?: number;
  preselectedCustomerDisplayName?: string;
};

function parsePreselectedCustomerState(value: unknown): PreselectedCustomerState {
  if (!value || typeof value !== "object") {
    return {};
  }

  const state = value as Record<string, unknown>;
  const parsedCustomerId = Number(state.preselectedCustomerId);

  return {
    preselectedCustomerId:
      Number.isFinite(parsedCustomerId) && parsedCustomerId > 0 ? parsedCustomerId : undefined,
    preselectedCustomerDisplayName:
      typeof state.preselectedCustomerDisplayName === "string" ? state.preselectedCustomerDisplayName : undefined,
  };
}

export default function AccountNew() {
  const location = useLocation();
  const navigate = useNavigate();
  const [customers, setCustomers] = useState<CustomerLookup[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [openingBalance, setOpeningBalance] = useState("0");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { preselectedCustomerId, preselectedCustomerDisplayName } = parsePreselectedCustomerState(location.state);

  const loadCustomers = useCallback(async () => {
    setIsLoading(true);

    try {
      const customersData = await customerService.getCustomerLookup();
      const hasPreselectedCustomer = preselectedCustomerId
        ? customersData.some((customer) => customer.id === preselectedCustomerId)
        : false;
      const defaultCustomerId = hasPreselectedCustomer
        ? preselectedCustomerId!.toString()
        : customersData[0]?.id?.toString() || "";

      setCustomers(customersData);
      setCustomerId(defaultCustomerId);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load customers"));
    } finally {
      setIsLoading(false);
    }
  }, [preselectedCustomerId]);

  useEffect(() => {
    void loadCustomers();
  }, [loadCustomers]);

  const handleCustomerChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = event.target;

    if (name === "customerId") {
      setCustomerId(value);
      return;
    }

    setOpeningBalance(value);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!customerId) {
      toast.error("Select a customer");
      return;
    }

    const parsedCustomerId = Number(customerId);
    const parsedOpeningBalance = Number(openingBalance);
    if (!Number.isFinite(parsedCustomerId) || parsedCustomerId <= 0) {
      toast.error("Invalid customer");
      return;
    }

    if (!Number.isFinite(parsedOpeningBalance) || parsedOpeningBalance < 0) {
      toast.error("Opening balance must be zero or higher");
      return;
    }

    const payload: CreateBankAccountRequest = {
      customerId: parsedCustomerId,
      openingBalance: parsedOpeningBalance,
    };

    setIsSubmitting(true);
    try {
      const createdAccount = await accountService.createAccount(payload);
      toast.success("Account opened");
      navigate(`/accounts/${createdAccount.id}`);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not create account"));
    } finally {
      setIsSubmitting(false);
    }
  };

  const renderCustomerOptions = () => {
    return customers.map((customer) => (
      <option key={customer.id} value={customer.id}>
        {customer.displayName}
      </option>
    ));
  };

  const selectedCustomer = customers.find((customer) => customer.id.toString() === customerId);

  if (isLoading) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading customers...</p>
      </section>
    );
  }

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <h1 className="text-3xl font-bold tracking-tight">Open Account</h1>
      {preselectedCustomerId ? (
        <p className="mt-1 text-sm text-secondary">
          Opening account for{" "}
          <span className="font-semibold text-foreground">
            {preselectedCustomerDisplayName || `Customer #${preselectedCustomerId}`}
          </span>
          .
        </p>
      ) : null}

      <form onSubmit={handleSubmit} className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4">
          <Dropdown label="Customer" name="customerId" value={customerId} onChange={handleCustomerChange} required>
            {renderCustomerOptions()}
          </Dropdown>

          {selectedCustomer ? (
            <div className="flex items-center gap-2 text-sm text-secondary">
              <span>Category</span>
              <VipBadge isVip={selectedCustomer.isVip} />
            </div>
          ) : null}

          <TextInputField
            label="Opening balance"
            name="openingBalance"
            type="number"
            min="0"
            step="0.01"
            value={openingBalance}
            onChange={handleCustomerChange}
            required
          />

          <p className="text-xs text-tertiary">IBAN is generated automatically after opening the account.</p>
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
        >
          {isSubmitting ? "Opening..." : "Open account"}
        </button>
      </form>
    </section>
  );
}

