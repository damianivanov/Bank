import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { creditConditionService } from "@/services/creditConditionService";
import { creditService } from "@/services/creditService";
import { customerService } from "@/services/customerService";
import { Dropdown, TextInputField, VipBadge } from "@/shared/components";
import {
  CreditType,
  type CreateCreditRequest,
  type CreditTypeCondition,
  type CustomerLookup,
} from "@/types";

export default function CreditNew() {
  const navigate = useNavigate();
  const [customers, setCustomers] = useState<CustomerLookup[]>([]);
  const [conditions, setConditions] = useState<CreditTypeCondition[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [creditType, setCreditType] = useState(CreditType.Consumer);
  const [grantedAmount, setGrantedAmount] = useState("");
  const [termMonths, setTermMonths] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadDependencies = useCallback(async () => {
    setIsLoading(true);

    try {
      const [customersData, conditionsData] = await Promise.all([
        customerService.getCustomerLookup(),
        creditConditionService.getCreditConditions(),
      ]);

      setCustomers(customersData);
      setConditions(conditionsData);
      setCustomerId(customersData[0]?.id?.toString() || "");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load dependencies"));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadDependencies();
  }, [loadDependencies]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const parsedCustomerId = Number(customerId);
    const parsedGrantedAmount = Number(grantedAmount);
    const parsedTermMonths = Number(termMonths);

    if (!Number.isFinite(parsedCustomerId) || parsedCustomerId <= 0) {
      toast.error("Select a customer");
      return;
    }

    if (!Number.isFinite(parsedGrantedAmount) || parsedGrantedAmount <= 0) {
      toast.error("Amount must be greater than zero");
      return;
    }

    if (!Number.isFinite(parsedTermMonths) || parsedTermMonths <= 0) {
      toast.error("Term months must be greater than zero");
      return;
    }

    const payload: CreateCreditRequest = {
      customerId: parsedCustomerId,
      creditType,
      grantedAmount: parsedGrantedAmount,
      termMonths: parsedTermMonths,
    };

    setIsSubmitting(true);
    try {
      const createdCredit = await creditService.createCredit(payload);
      toast.success("Credit granted");
      navigate(`/credits/${createdCredit.id}`);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not grant credit"));
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedCustomer = useMemo(() => {
    return customers.find((customer) => customer.id.toString() === customerId) || null;
  }, [customers, customerId]);

  const selectedCondition = useMemo(() => {
    return conditions.find((condition) => condition.creditType === creditType) || null;
  }, [conditions, creditType]);

  const previewRate = selectedCondition
    ? selectedCustomer?.isVip
      ? selectedCondition.vipAnnualInterestRate
      : selectedCondition.standardAnnualInterestRate
    : null;

  const previewFee = selectedCondition
    ? selectedCustomer?.isVip
      ? selectedCondition.vipGrantingFee
      : selectedCondition.standardGrantingFee
    : null;

  if (isLoading) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading data...</p>
      </section>
    );
  }

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <h1 className="text-3xl font-bold tracking-tight">Grant Credit</h1>

      <form onSubmit={handleSubmit} className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4 md:grid-cols-2">
          <Dropdown
            label="Customer"
            name="customerId"
            value={customerId}
            onChange={(event) => setCustomerId(event.target.value)}
            required
            className="md:col-span-2"
          >
            {customers.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.displayName}
              </option>
            ))}
          </Dropdown>

          <Dropdown label="Credit type" name="creditType" value={creditType} onChange={(event) => setCreditType(Number(event.target.value) as CreditType)}>
            <option value={CreditType.Consumer}>Consumer</option>
            <option value={CreditType.Mortgage}>Mortgage</option>
          </Dropdown>

          <TextInputField
            label="Amount"
            name="grantedAmount"
            type="number"
            step="0.01"
            min="0.01"
            value={grantedAmount}
            onChange={(event) => setGrantedAmount(event.target.value)}
            required
          />

          <TextInputField
            label="Term months"
            name="termMonths"
            type="number"
            step="1"
            min="1"
            value={termMonths}
            onChange={(event) => setTermMonths(event.target.value)}
            required
          />

          <div className="bank-panel rounded-xl p-4 md:col-span-2">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-sm text-secondary">Customer category</span>
              <VipBadge isVip={Boolean(selectedCustomer?.isVip)} />
            </div>
            {selectedCondition ? (
              <div className="mt-3 grid gap-2 text-sm sm:grid-cols-2">
                <p>Max amount: <span className="font-semibold">{formatCurrency(selectedCondition.maximumAmount)}</span></p>
                <p>Max term: <span className="font-semibold">{selectedCondition.maximumTermMonths} months</span></p>
                <p>Applied annual rate: <span className="font-semibold">{previewRate === null ? "-" : formatPercent(previewRate)}</span></p>
                <p>Applied granting fee: <span className="font-semibold">{previewFee === null ? "-" : formatCurrency(previewFee)}</span></p>
              </div>
            ) : (
              <p className="mt-3 text-sm text-secondary">Select credit type to see limits.</p>
            )}
          </div>
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
        >
          {isSubmitting ? "Granting..." : "Grant credit"}
        </button>
      </form>
    </section>
  );
}

