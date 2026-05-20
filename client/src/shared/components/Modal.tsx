import type { ReactNode } from "react";
import { X } from "lucide-react";

type ModalProps = {
  title: string;
  isOpen: boolean;
  children: ReactNode;
  onClose: () => void;
};

export default function Modal({ title, isOpen, children, onClose }: ModalProps) {
  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button type="button" className="bank-overlay absolute inset-0" onClick={onClose} aria-label="Close modal" />
      <section className="bank-panel relative z-10 w-full max-w-xl rounded-2xl p-5">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="bank-secondary-btn inline-flex h-9 w-9 items-center justify-center rounded-full"
            aria-label="Close"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        {children}
      </section>
    </div>
  );
}
