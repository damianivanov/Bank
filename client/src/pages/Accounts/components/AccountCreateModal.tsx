import { Plus } from "lucide-react";
import { Dropdown, Modal, MoneyInputField } from "@/shared/components";
import { useAccountCreateModal } from "../hooks/useAccountCreateModal";

type AccountCreateModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (accountId: number) => void;
  presetCustomerId?: number;
  presetCustomerDisplayName?: string;
};

export default function AccountCreateModal({
  isOpen,
  onClose,
  onCreated,
  presetCustomerId,
  presetCustomerDisplayName,
}: AccountCreateModalProps) {
  const { state, actions } = useAccountCreateModal({ isOpen, presetCustomerId, onClose, onCreated });

  return (
    <Modal title="Открий сметка" isOpen={isOpen} onClose={actions.close}>
      {state.hasPreset ? (
        <p className="mb-4 text-sm text-secondary">
          Откриване на сметка за <span className="font-semibold text-foreground">{presetCustomerDisplayName}</span>.
        </p>
      ) : null}

      <form onSubmit={actions.submit}>
        {!state.hasPreset ? (
          <div className="mb-4">
            <Dropdown
              label="Клиент"
              name="customerId"
              value={state.customerId}
              onChange={(event) => actions.setCustomerId(event.target.value)}
              onSearchChange={actions.setCustomerSearch}
              loading={state.isCustomerLoading}
              searchPlaceholder="Търсене на клиент..."
              error={state.errors.customerId}
            >
              {state.customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.displayName}
                </option>
              ))}
            </Dropdown>
          </div>
        ) : null}

        <MoneyInputField
          label="Начално салдо"
          name="openingBalance"
          value={state.openingBalance}
          onValueChange={actions.setOpeningBalance}
          error={state.errors.openingBalance}
        />

        <p className="mt-3 text-xs text-tertiary">IBAN се генерира автоматично след откриване на сметката.</p>

        <button
          type="submit"
          disabled={state.isSubmitting}
          className="bank-primary-btn mt-5 bank-btn disabled:opacity-60"
        >
          <Plus className="h-4 w-4" />
          {state.isSubmitting ? "Откриване..." : "Открий сметка"}
        </button>
      </form>
    </Modal>
  );
}
