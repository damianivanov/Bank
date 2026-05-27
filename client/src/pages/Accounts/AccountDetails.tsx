import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { accountService } from "@/services/accountService";
import { AccountStatusBadge } from "@/shared/components";
import { BankAccountStatus, type BankAccountDetails } from "@/types";

export default function AccountDetailsPage() {
  const { accountId } = useParams();
  const navigate = useNavigate();
  const parsedAccountId = Number(accountId);

  const [account, setAccount] = useState<BankAccountDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isClosing, setIsClosing] = useState(false);

  const loadAccount = useCallback(async () => {
    if (!Number.isFinite(parsedAccountId) || parsedAccountId <= 0) {
      toast.error("Invalid account id");
      navigate("/accounts", { replace: true });
      return;
    }

    setIsLoading(true);

    try {
      const accountDetails = await accountService.getAccount(parsedAccountId);
      setAccount(accountDetails);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load account"));
      navigate("/accounts", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedAccountId]);

  useEffect(() => {
    void loadAccount();
  }, [loadAccount]);

  const handleCloseAccount = async () => {
    if (!account) {
      return;
    }

    setIsClosing(true);
    try {
      const closedAccount = await accountService.closeAccount(account.id);
      setAccount(closedAccount);
      toast.success("Account closed");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not close account"));
    } finally {
      setIsClosing(false);
    }
  };

  if (isLoading || !account) {
    return (
      <section className="w-full px-4 py-6 md:px-8">
        <p className="text-sm text-secondary">Loading account...</p>
      </section>
    );
  }

  const canClose = account.status === BankAccountStatus.Active && account.balance === 0;

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Account Details</h1>
          <p className="mt-1 font-mono text-xs text-secondary">{account.iban}</p>
        </div>
        <div className="flex items-center gap-2">
          <Link to={`/customers/${account.customerId}`} className="bank-secondary-btn rounded-xl px-4 py-2 text-sm font-semibold">
            Customer
          </Link>
          <button
            type="button"
            onClick={handleCloseAccount}
            disabled={!canClose || isClosing}
            className="bank-danger-btn rounded-xl px-4 py-2 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isClosing ? "Closing..." : "Close account"}
          </button>
        </div>
      </div>

      <section className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Customer</p>
            <p className="mt-1 text-sm font-semibold">{account.customerDisplayName}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Status</p>
            <div className="mt-2">
              <AccountStatusBadge status={account.status} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Balance</p>
            <p className="mt-1 text-lg font-bold">{formatCurrency(account.balance)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Opened</p>
            <p className="mt-1 text-sm">{formatDate(account.openedAtUtc)}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Closed</p>
            <p className="mt-1 text-sm">{formatDate(account.closedAtUtc)}</p>
          </div>
        </div>
      </section>
    </section>
  );
}

