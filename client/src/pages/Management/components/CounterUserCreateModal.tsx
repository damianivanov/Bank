import { Modal } from "@/shared/components";
import { useCounterUserCreateModal } from "../hooks/useCounterUserCreateModal";
import CounterUserForm from "./CounterUserForm";

type CounterUserCreateModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
};

export default function CounterUserCreateModal({ isOpen, onClose, onCreated }: CounterUserCreateModalProps) {
  const { state, actions } = useCounterUserCreateModal({ onClose, onCreated });

  return (
    <Modal title="Нов потребител" isOpen={isOpen} onClose={actions.close} widthClassName="max-w-2xl">
      <p className="mb-3 text-sm text-secondary">
        Създава логин акаунт и клиент. Клиентът ще бъде помолен да смени паролата си при първо влизане.
      </p>
      <CounterUserForm isSubmitting={state.isSubmitting} onSubmit={actions.submit} />
    </Modal>
  );
}
