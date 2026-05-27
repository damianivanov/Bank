import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { creditService } from "@/services/creditService";
import {
  CreditStatusBadge,
  EntityGrid,
  PaymentStatusBadge,
  VipBadge,
} from "@/shared/components";
import { CreditPaymentStatus, CreditStatus, type CreditDetails } from "@/types";

export default function CreditDetailsPage() {
  const { creditId } = useParams();
  const navigate = useNavigate();
  const parsedCreditId = Number(creditId);

  const [credit, setCredit] = useState<CreditDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isPaying, setIsPaying] = useState(false);

  const loadCredit = useCallback(async () => {
    if (!Number.isFinite(parsedCreditId) || parsedCreditId <= 0) {
      toast.error("Invalid credit id");
      navigate("/credits", { replace: true });
      return;
    }

    setIsLoading(true);
    try {
      const creditDetails = await creditService.getCredit(parsedCreditId);
      setCredit(creditDetails);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load credit"));
      navigate("/credits", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedCreditId]);

  useEffect(() => {
    void loadCredit();
  }, [loadCredit]);

  const nextPendingPayment = useMemo(() => {
    if (!credit) {
      return null;
    }

    return credit.payments.find((payment) => payment.status === CreditPaymentStatus.Pending) ?? null;
  }, [credit]);

  const handlePayNextPayment = async () => {
    if (!credit || !nextPendingPayment) {
      return;
    }

    setIsPaying(true);
    try {
      const updatedCredit = await creditService.payPayment(credit.id, nextPendingPayment.id);
      setCredit(updatedCredit);
      toast.success("Payment registered");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not pay payment"));
    } finally {
      setIsPaying(false);
    }
  };

  if (isLoading || !credit) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading credit...</p>
      </section>
    );
  }

  const isActiveCredit = credit.status === CreditStatus.Active;

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Credit Details</h1>
          <p className="mt-1 text-sm text-secondary">{credit.customerDisplayName}</p>
        </div>
        <div className="flex items-center gap-2">
          <Link to={`/customers/${credit.customerId}`} className="bank-secondary-btn rounded-xl px-4 py-2 text-sm font-semibold">
            Customer
          </Link>
          <button
            type="button"
            onClick={handlePayNextPayment}
            disabled={!isActiveCredit || !nextPendingPayment || isPaying}
            className="bank-primary-btn rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
          >
            {isPaying ? "Paying..." : "Pay next payment"}
          </button>
        </div>
      </div>

      <section className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4 md:grid-cols-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Credit type</p>
            <p className="mt-1 text-sm font-semibold">{credit.creditType === 1 ? "Consumer" : "Mortgage"}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Status</p>
            <div className="mt-2">
              <CreditStatusBadge status={credit.status} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">VIP at creation</p>
            <div className="mt-2">
              <VipBadge isVip={credit.customerWasVipAtCreation} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Granted amount</p>
            <p className="mt-1 text-sm font-semibold">{formatCurrency(credit.grantedAmount)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Term</p>
            <p className="mt-1 text-sm font-semibold">{credit.termMonths} months</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Planned payment</p>
            <p className="mt-1 text-sm font-semibold">{formatCurrency(credit.plannedMonthlyPaymentAmount)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Applied annual rate</p>
            <p className="mt-1 text-sm font-semibold">{formatPercent(credit.appliedAnnualInterestRate)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Current annual rate</p>
            <p className="mt-1 text-sm font-semibold">{formatPercent(credit.currentAnnualInterestRate)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Granting fee</p>
            <p className="mt-1 text-sm font-semibold">{formatCurrency(credit.appliedGrantingFee)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Granted on</p>
            <p className="mt-1 text-sm">{formatDate(credit.grantedAtUtc)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Repaid on</p>
            <p className="mt-1 text-sm">{formatDate(credit.repaidAtUtc)}</p>
          </div>
        </div>
      </section>

      {credit.lastPricingChange ? (
        <section className="bank-panel mt-6 rounded-2xl p-5">
          <h2 className="text-lg font-bold tracking-tight">Last Pricing Change</h2>
          <div className="mt-3 grid gap-2 text-sm sm:grid-cols-2">
            <p>
              Previous rate: <span className="font-semibold">{formatPercent(credit.lastPricingChange.previousAnnualInterestRate)}</span>
            </p>
            <p>
              New rate: <span className="font-semibold">{formatPercent(credit.lastPricingChange.newAnnualInterestRate)}</span>
            </p>
            <p>
              Effective payment: <span className="font-semibold">#{credit.lastPricingChange.effectiveFromPaymentNumber}</span>
            </p>
            <p>
              Changed on: <span className="font-semibold">{formatDate(credit.lastPricingChange.dateCreated)}</span>
            </p>
          </div>
        </section>
      ) : null}

      <section className="mt-6">
        <h2 className="mb-3 text-xl font-bold tracking-tight">Repayment Plan</h2>
        <EntityGrid>
          <thead>
            <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
              <th className="px-4 py-3">#</th>
              <th className="px-4 py-3">Due date</th>
              <th className="px-4 py-3">Payment</th>
              <th className="px-4 py-3">Principal</th>
              <th className="px-4 py-3">Interest</th>
              <th className="px-4 py-3">Remaining principal</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Paid at</th>
            </tr>
          </thead>
          <tbody>
            {credit.payments.map((payment) => (
              <tr key={payment.id} className="border-b border-slate-100 text-sm last:border-b-0">
                <td className="px-4 py-3 font-semibold">{payment.paymentNumber}</td>
                <td className="px-4 py-3">{formatDate(payment.dueDate)}</td>
                <td className="px-4 py-3">{formatCurrency(payment.paymentAmount)}</td>
                <td className="px-4 py-3">{formatCurrency(payment.principalPart)}</td>
                <td className="px-4 py-3">{formatCurrency(payment.interestPart)}</td>
                <td className="px-4 py-3">{formatCurrency(payment.remainingPrincipalAfterPayment)}</td>
                <td className="px-4 py-3">
                  <PaymentStatusBadge status={payment.status} />
                </td>
                <td className="px-4 py-3">{formatDate(payment.paidAtUtc)}</td>
              </tr>
            ))}
          </tbody>
        </EntityGrid>
      </section>
    </section>
  );
}

