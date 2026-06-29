import { Check, X } from "lucide-react";
import { Modal } from "@/shared/components";
import type { DepositRequestQueue } from "@/types";
import DepositRequestSummary from "./DepositRequestSummary";

type ApproveDepositModalProps = {
  request: DepositRequestQueue | null;
  isSubmitting: boolean;
  onClose: () => void;
  onConfirm: () => void;
};

export default function ApproveDepositModal({ request, isSubmitting, onClose, onConfirm }: ApproveDepositModalProps) {
  if (!request) {
    return null;
  }

  return (
    <Modal title="Одобряване на заявка за депозит" isOpen={request !== null} onClose={onClose}>
      <p className="mb-4 text-sm text-secondary">
        Сигурни ли сте, че искате да одобрите тази заявка? Салдото ще бъде увеличено със сумата по-долу.
      </p>

      <DepositRequestSummary request={request} />

      <div className="mt-5 flex items-center justify-end gap-2">
        <button
          type="button"
          onClick={onClose}
          disabled={isSubmitting}
          className="bank-secondary-btn bank-btn disabled:opacity-60"
        >
          <X className="h-4 w-4" />
          Отказ
        </button>
        <button
          type="button"
          onClick={onConfirm}
          disabled={isSubmitting}
          className="bank-primary-btn bank-btn disabled:opacity-60"
        >
          <Check className="h-4 w-4" />
          {isSubmitting ? "Одобряване..." : "Одобри заявката"}
        </button>
      </div>
    </Modal>
  );
}
