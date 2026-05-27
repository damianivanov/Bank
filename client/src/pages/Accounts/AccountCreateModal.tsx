import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { accountService } from "@/services/accountService";
import { Modal, TextInputField } from "@/shared/components";
import type { CreateBankAccountRequest } from "@/types";

type AccountCreateModalProps = {
  isOpen: boolean;
  customerId: number;
  customerDisplayName: string;
  onClose: () => void;
  onCreated?: (accountId: number) => void;
};

type AccountForm = {
  openingBalance: string;
};

const initialForm: AccountForm = {
  openingBalance: "0",
};

export default function AccountCreateModal({
  isOpen,
  customerId,
  customerDisplayName,
  onClose,
  onCreated,
}: AccountCreateModalProps) {
  const [form, setForm] = useState<AccountForm>(initialForm);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setForm(initialForm);
  }, [isOpen]);

  const handleFieldChange = (event: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  };

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

    const parsedOpeningBalance = Number(form.openingBalance);
    if (!Number.isFinite(parsedOpeningBalance) || parsedOpeningBalance < 0) {
      toast.error("Opening balance must be zero or higher");
      return;
    }

    const payload: CreateBankAccountRequest = {
      customerId,
      openingBalance: parsedOpeningBalance,
    };

    setIsSubmitting(true);
    try {
      const createdAccount = await accountService.createAccount(payload);
      toast.success("Account opened");
      onCreated?.(createdAccount.id);
      onClose();
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not create account"));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title="Open account" isOpen={isOpen} onClose={handleClose}>
      <p className="mb-4 text-sm text-secondary">
        Opening account for <span className="font-semibold text-foreground">{customerDisplayName}</span>.
      </p>

      <form onSubmit={handleSubmit}>
        <TextInputField
          label="Opening balance"
          name="openingBalance"
          type="number"
          min="0"
          step="0.01"
          value={form.openingBalance}
          onChange={handleFieldChange}
          required
        />

        <p className="mt-3 text-xs text-tertiary">IBAN is generated automatically after opening the account.</p>

        <button
          type="submit"
          disabled={isSubmitting}
          className="bank-primary-btn mt-5 rounded-xl px-4 py-2 text-sm font-semibold disabled:opacity-60"
        >
          {isSubmitting ? "Opening..." : "Open account"}
        </button>
      </form>
    </Modal>
  );
}
