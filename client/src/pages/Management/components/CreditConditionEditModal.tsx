import { Check, X } from "lucide-react";
import { Modal, MoneyInputField, NumberInputField } from "@/shared/components";
import type { CreditTypeCondition } from "@/types";
import { useCreditConditionEditModal } from "../hooks/useCreditConditionEditModal";

type CreditConditionEditModalProps = {
  condition: CreditTypeCondition;
  onClose: () => void;
  onSaved: () => void;
};

export default function CreditConditionEditModal({ condition, onClose, onSaved }: CreditConditionEditModalProps) {
  const { state, actions } = useCreditConditionEditModal({ condition, onSaved, onClose });
  const { fields, errors } = state;

  return (
    <Modal title={`Редакция: ${condition.name}`} isOpen onClose={actions.close}>
      <p className="mb-4 text-sm text-secondary">
        Промяната важи за нови кредити. Съществуващите кредити запазват договорните си условия.
      </p>

      <div className="grid gap-4 sm:grid-cols-2">
        <NumberInputField
          label="Стандартен процент"
          suffix="%"
          value={fields.standardAnnualInterestRate}
          error={errors.standardAnnualInterestRate}
          onValueChange={(raw) => actions.setField("standardAnnualInterestRate", raw)}
        />
        <NumberInputField
          label="VIP процент"
          suffix="%"
          value={fields.vipAnnualInterestRate}
          error={errors.vipAnnualInterestRate}
          onValueChange={(raw) => actions.setField("vipAnnualInterestRate", raw)}
        />
        <MoneyInputField
          label="Макс. сума"
          value={fields.maximumAmount}
          error={errors.maximumAmount}
          onValueChange={(raw) => actions.setField("maximumAmount", raw)}
        />
        <NumberInputField
          label="Макс. срок"
          suffix="мес."
          value={fields.maximumTermMonths}
          error={errors.maximumTermMonths}
          onValueChange={(raw) => actions.setField("maximumTermMonths", raw)}
        />
        <MoneyInputField
          label="Стандартна такса"
          value={fields.standardGrantingFee}
          error={errors.standardGrantingFee}
          onValueChange={(raw) => actions.setField("standardGrantingFee", raw)}
        />
        <MoneyInputField
          label="VIP такса"
          value={fields.vipGrantingFee}
          error={errors.vipGrantingFee}
          onValueChange={(raw) => actions.setField("vipGrantingFee", raw)}
        />
      </div>

      <div className="mt-5 flex items-center justify-end gap-2">
        <button
          type="button"
          onClick={actions.close}
          disabled={state.isSubmitting}
          className="bank-secondary-btn bank-btn disabled:opacity-60"
        >
          <X className="h-4 w-4" />
          Отказ
        </button>
        <button
          type="button"
          onClick={actions.submit}
          disabled={state.isSubmitting}
          className="bank-primary-btn bank-btn disabled:opacity-60"
        >
          <Check className="h-4 w-4" />
          {state.isSubmitting ? "Запазване..." : "Запази"}
        </button>
      </div>
    </Modal>
  );
}
