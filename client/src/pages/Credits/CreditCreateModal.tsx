import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { creditConditionService } from "@/services/creditConditionService";
import { creditService } from "@/services/creditService";
import { Dropdown, Modal, TextInputField } from "@/shared/components";
import { CreditType, type CreateCreditRequest, type CreditTypeCondition } from "@/types";

type CreditCreateModalProps = {
  isOpen: boolean;
  customerId: number;
  customerDisplayName: string;
  onClose: () => void;
  onCreated?: (creditId: number) => void;
};

export default function CreditCreateModal({
  isOpen,
  customerId,
  customerDisplayName,
  onClose,
  onCreated,
}: CreditCreateModalProps) {
  const [conditions, setConditions] = useState<CreditTypeCondition[]>([]);
  const [creditType, setCreditType] = useState(CreditType.Consumer);
  const [grantedAmount, setGrantedAmount] = useState("");
  const [termMonths, setTermMonths] = useState("");
  const [isLoadingConditions, setIsLoadingConditions] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadConditions = useCallback(async () => {
    setIsLoadingConditions(true);

    try {
      const loadedConditions = await creditConditionService.getCreditConditions();
      setConditions(loadedConditions.filter((condition) => condition.isActive));
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load credit conditions"));
    } finally {
      setIsLoadingConditions(false);
    }
  }, []);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setCreditType(CreditType.Consumer);
    setGrantedAmount("");
    setTermMonths("");
    void loadConditions();
  }, [isOpen, loadConditions]);

  const selectedCondition = useMemo(() => {
    return conditions.find((condition) => condition.creditType === creditType) ?? null;
  }, [conditions, creditType]);

  const handleClose = () => {
    if (isSubmitting) {
      return;
    }

    onClose();
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!Number.isFinite(customerId) || customerId <= 0) {
      toast.error("Invalid customer");
      return;
    }

    const parsedGrantedAmount = Number(grantedAmount);
    const parsedTermMonths = Number(termMonths);

    if (!Number.isFinite(parsedGrantedAmount) || parsedGrantedAmount <= 0) {
      toast.error("Amount must be greater than zero");
      return;
    }

    if (!Number.isFinite(parsedTermMonths) || parsedTermMonths <= 0) {
      toast.error("Term months must be greater than zero");
      return;
    }

    if (selectedCondition) {
      if (parsedGrantedAmount > selectedCondition.maximumAmount) {
        toast.error(`Amount exceeds maximum (${formatCurrency(selectedCondition.maximumAmount)})`);
        return;
      }

      if (parsedTermMonths > selectedCondition.maximumTermMonths) {
        toast.error(`Term exceeds maximum (${selectedCondition.maximumTermMonths} months)`);
        return;
      }
    }

    const payload: CreateCreditRequest = {
      customerId,
      creditType,
      grantedAmount: parsedGrantedAmount,
      termMonths: parsedTermMonths,
    };

    setIsSubmitting(true);
    try {
      const createdCredit = await creditService.createCredit(payload);
      toast.success("Credit granted");
      onCreated?.(createdCredit.id);
      onClose();
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not grant credit"));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title="Grant credit" isOpen={isOpen} onClose={handleClose}>
      <p className="mb-4 text-sm text-secondary">
        Granting credit for <span className="font-semibold text-foreground">{customerDisplayName}</span>.
      </p>

      {isLoadingConditions ? (
        <p className="text-sm text-secondary">Loading credit conditions...</p>
      ) : (
        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <Dropdown
              label="Credit type"
              name="creditType"
              value={creditType}
              onChange={(event) => setCreditType(Number(event.target.value) as CreditType)}
              className="md:col-span-2"
            >
              <option value={CreditType.Consumer}>Consumer</option>
              <option value={CreditType.Mortgage}>Mortgage</option>
            </Dropdown>

            <TextInputField
              label="Amount"
              name="grantedAmount"
              type="number"
              min="0.01"
              step="0.01"
              value={grantedAmount}
              onChange={(event) => setGrantedAmount(event.target.value)}
              required
            />

            <TextInputField
              label="Term months"
              name="termMonths"
              type="number"
              min="1"
              step="1"
              value={termMonths}
              onChange={(event) => setTermMonths(event.target.value)}
              required
            />
          </div>

          {selectedCondition ? (
            <div className="bank-panel mt-4 rounded-xl p-4 text-sm">
              <div className="grid gap-2 sm:grid-cols-2">
                <p>
                  Max amount: <span className="font-semibold">{formatCurrency(selectedCondition.maximumAmount)}</span>
                </p>
                <p>
                  Max term: <span className="font-semibold">{selectedCondition.maximumTermMonths} months</span>
                </p>
                <p>
                  Standard rate: <span className="font-semibold">{formatPercent(selectedCondition.standardAnnualInterestRate)}</span>
                </p>
                <p>
                  VIP rate: <span className="font-semibold">{formatPercent(selectedCondition.vipAnnualInterestRate)}</span>
                </p>
              </div>
            </div>
          ) : null}

          <button
            type="submit"
            disabled={isSubmitting}
            className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
          >
            {isSubmitting ? "Granting..." : "Grant credit"}
          </button>
        </form>
      )}
    </Modal>
  );
}
