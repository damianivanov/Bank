import { Trash2, X } from "lucide-react";
import Modal from "./Modal";

type DeleteConfirmationModalProps = {
  isOpen: boolean;
  itemName?: string;
  title?: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isDeleting?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
};

export default function DeleteConfirmationModal({
  isOpen,
  itemName,
  title = "Изтриване",
  description = "Това действие е необратимо.",
  confirmLabel = "Изтрий",
  cancelLabel = "Отказ",
  isDeleting = false,
  onCancel,
  onConfirm,
}: DeleteConfirmationModalProps) {
  const handleCancel = () => {
    if (isDeleting) {
      return;
    }

    onCancel();
  };

  const handleConfirm = () => {
    if (isDeleting) {
      return;
    }

    onConfirm();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleCancel} title={title}>
      <p className="text-sm">
        {itemName ? (
          <>
            Да изтрия ли <span className="font-semibold">„{itemName}“</span>?
          </>
        ) : (
          "Да изтрия ли този елемент?"
        )}
      </p>
      <p className="mt-2 text-xs text-secondary">{description}</p>

      <div className="mt-6 flex justify-end gap-2">
        <button
          type="button"
          onClick={handleCancel}
          disabled={isDeleting}
          className="bank-secondary-btn bank-btn disabled:cursor-not-allowed disabled:opacity-60"
        >
          <X className="h-4 w-4" />
          {cancelLabel}
        </button>
        <button
          type="button"
          onClick={handleConfirm}
          disabled={isDeleting}
          className="bank-danger-btn bank-btn disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Trash2 className="h-4 w-4" />
          {isDeleting ? "Изтриване..." : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}
