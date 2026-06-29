import { Modal } from "@/shared/components";
import { type Customer } from "@/types";
import { useCustomerUpsertModal } from "../hooks/useCustomerUpsertModal";
import { type LinkUserContext } from "../utils/customerForm";
import CustomerForm from "./CustomerForm";

export type { LinkUserContext };

type CustomerUpsertModalProps = {
  isOpen: boolean;
  customerId?: number;
  linkUserContext?: LinkUserContext | null;
  widthClassName?: string;
  onClose: () => void;
  onSaved?: (customer: Customer) => void;
};

export default function CustomerUpsertModal({
  isOpen,
  customerId,
  linkUserContext,
  widthClassName = "max-w-2xl",
  onClose,
  onSaved,
}: CustomerUpsertModalProps) {
  const { state, actions } = useCustomerUpsertModal({
    isOpen,
    customerId,
    linkUserContext,
    onClose,
    onSaved,
  });

  return (
    <Modal title={state.modalTitle} isOpen={isOpen} onClose={actions.close} widthClassName={widthClassName}>
      {state.linkedUserDisplayName ? (
        <p className="mb-2 text-sm text-secondary">
          Този клиент ще бъде свързан с{" "}
          <span className="font-semibold text-foreground">{state.linkedUserDisplayName}</span>.
        </p>
      ) : null}

      {state.isEditMode && state.isLoadingCustomer ? (
        <p className="text-sm text-secondary">Зареждане на клиент...</p>
      ) : (
        <CustomerForm
          key={`${customerId ?? "new"}-${linkUserContext?.linkUserId ?? "none"}`}
          initialValue={state.initialValue}
          submitLabel={state.isEditMode ? "Запази промените" : "Създай клиент"}
          isSubmitting={state.isSubmitting}
          showPanel={false}
          onSubmit={actions.submit}
        />
      )}
    </Modal>
  );
}
