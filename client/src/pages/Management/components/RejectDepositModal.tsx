import { useEffect, useState } from "react";
import { X } from "lucide-react";
import { Modal } from "@/shared/components";
import type { DepositRequestQueue } from "@/types";
import DepositRequestSummary from "./DepositRequestSummary";

type RejectDepositModalProps = {
  request: DepositRequestQueue | null;
  isSubmitting: boolean;
  onClose: () => void;
  onConfirm: (note: string) => void;
};

export default function RejectDepositModal({ request, isSubmitting, onClose, onConfirm }: RejectDepositModalProps) {
  const [note, setNote] = useState("");

  useEffect(() => {
    if (request) {
      setNote("");
    }
  }, [request]);

  if (!request) {
    return null;
  }

  return (
    <Modal title="Отхвърляне на заявка за депозит" isOpen={request !== null} onClose={onClose}>
      <p className="mb-4 text-sm text-secondary">
        Сигурни ли сте, че искате да отхвърлите тази заявка? Салдото няма да бъде променено.
      </p>

      <DepositRequestSummary request={request} />

      <div className="mt-4">
        <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-tertiary" htmlFor="reject-note">
          Бележка (по избор)
        </label>
        <textarea
          id="reject-note"
          value={note}
          onChange={(event) => setNote(event.target.value)}
          rows={3}
          maxLength={500}
          className="bank-input w-full resize-none rounded-xl px-3 py-2.5 text-sm"
          placeholder="Причина за отхвърлянето..."
        />
      </div>

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
          onClick={() => onConfirm(note)}
          disabled={isSubmitting}
          className="bank-danger-solid-btn bank-btn disabled:opacity-60"
        >
          <X className="h-4 w-4" />
          {isSubmitting ? "Отхвърляне..." : "Отхвърли заявката"}
        </button>
      </div>
    </Modal>
  );
}
