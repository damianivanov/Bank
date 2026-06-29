import { Calculator, Plus } from "lucide-react";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { CollapsibleSection, Dropdown, Modal, MoneyInputField, TextInputField } from "@/shared/components";
import FeeField from "@/pages/Calculators/components/FeeField";
import PaymentScheduleTable from "@/pages/Calculators/components/PaymentScheduleTable";
import ResultStat from "@/pages/Calculators/components/ResultStat";
import { CreditType, PaymentType } from "@/types";
import { useCreditIssuanceModal } from "../hooks/useCreditIssuanceModal";

type CreditCreateModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (creditId: number) => void;
  presetCustomerId?: number;
  presetCustomerIsVip?: boolean;
};

export default function CreditCreateModal({
  isOpen,
  onClose,
  onCreated,
  presetCustomerId,
  presetCustomerIsVip,
}: CreditCreateModalProps) {
  const { state, actions } = useCreditIssuanceModal({
    isOpen,
    presetCustomerId,
    presetCustomerIsVip,
    onClose,
    onCreated,
  });
  const { fields } = state;

  return (
    <Modal title="Отпусни кредит" isOpen={isOpen} onClose={actions.close} widthClassName="max-w-[min(90vw,96rem)]">
      <div className="mb-4 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        {!state.hasPreset ? (
          <div className="w-full sm:max-w-xs">
            <Dropdown
              label="Клиент"
              value={state.customerId}
              onChange={(event) => actions.selectCustomer(event.target.value)}
              onSearchChange={actions.setCustomerSearch}
              loading={state.isCustomerLoading}
              searchPlaceholder="Търсене на клиент..."
            >
              {state.customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.displayName}
                </option>
              ))}
            </Dropdown>
          </div>
        ) : null}

        <div className="w-full sm:w-56">
          <Dropdown
            label="Вид кредит"
            value={state.creditType}
            onChange={(event) => actions.setCreditType(Number(event.target.value) as CreditType)}
          >
            <option value={CreditType.Consumer}>Потребителски</option>
            <option value={CreditType.Mortgage}>Ипотечен</option>
          </Dropdown>
        </div>
      </div>

      <div className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <MoneyInputField
            label="Размер на кредита"
            value={fields.loanAmount}
            error={state.errors.loanAmount}
            onValueChange={actions.setLoanAmount}
          />
          <TextInputField
            label="Срок (месеци)"
            type="number"
            min="1"
            step="1"
            value={fields.termInMonths}
            error={state.errors.termInMonths}
            onChange={(event) => actions.setTermInMonths(event.target.value)}
          />
          <TextInputField
            label="Годишен лихвен процент (%)"
            type="number"
            min="0"
            step="0.01"
            value={fields.interestRate}
            error={state.errors.interestRate}
            onChange={(event) => actions.setInterestRate(event.target.value)}
          />
          <Dropdown
            label="Погасителен план"
            value={fields.paymentType}
            onChange={(event) => actions.setPaymentType(Number(event.target.value) as PaymentType)}
          >
            <option value={PaymentType.Annuity}>Анюитетен</option>
            <option value={PaymentType.Declining}>Намаляващи вноски</option>
          </Dropdown>
        </div>

        <CollapsibleSection title="Промоционален и гратисен период" description="По избор — оставете празно, за да пропуснете">
          <div className="grid gap-4 md:grid-cols-3">
            <TextInputField
              label="Промоционален период (месеци)"
              type="number"
              min="0"
              step="1"
              value={fields.promoPeriod}
              error={state.errors.promoPeriod}
              onChange={(event) => actions.setPromoPeriod(event.target.value)}
            />
            <TextInputField
              label="Промоционална лихва (%)"
              type="number"
              min="0"
              step="0.01"
              value={fields.promoRate}
              error={state.errors.promoRate}
              onChange={(event) => actions.setPromoRate(event.target.value)}
            />
            <TextInputField
              label="Гратисен период (месеци)"
              type="number"
              min="0"
              step="1"
              value={fields.gracePeriod}
              error={state.errors.gracePeriod}
              onChange={(event) => actions.setGracePeriod(event.target.value)}
            />
          </div>
        </CollapsibleSection>

        <CollapsibleSection title="Такси" description="По избор: еднократни, годишни и месечни такси">
          {/* @container: полетата се подреждат според ширината на модала, не на прозореца */}
          <div className="@container space-y-8">
            <div className="space-y-4">
              <p className="text-sm font-bold uppercase tracking-wide text-secondary">Първоначални такси</p>
              <div className="grid gap-4 @3xl:grid-cols-3">
                <FeeField label="Такса за кандидатстване" fee={fields.applicationFee} onChange={actions.setApplicationFee} />
                <FeeField label="Такса за обработка" fee={fields.processingFee} onChange={actions.setProcessingFee} />
                <FeeField label="Други първоначални такси" fee={fields.otherInitialFees} onChange={actions.setOtherInitialFees} />
              </div>
            </div>
            <div className="space-y-4">
              <p className="text-sm font-bold uppercase tracking-wide text-secondary">Годишни такси</p>
              <div className="grid gap-4 @xl:grid-cols-2">
                <FeeField label="Годишна такса за управление" fee={fields.annualManagementFee} onChange={actions.setAnnualManagementFee} />
                <FeeField label="Други годишни такси" fee={fields.otherAnnualFees} onChange={actions.setOtherAnnualFees} />
              </div>
            </div>
            <div className="space-y-4">
              <p className="text-sm font-bold uppercase tracking-wide text-secondary">Месечни такси</p>
              <div className="grid gap-4 @xl:grid-cols-2">
                <FeeField label="Месечна такса за управление" fee={fields.monthlyManagementFee} onChange={actions.setMonthlyManagementFee} />
                <FeeField label="Други месечни такси" fee={fields.otherMonthlyFees} onChange={actions.setOtherMonthlyFees} />
              </div>
            </div>
          </div>
        </CollapsibleSection>

        <div className="flex flex-wrap items-center justify-end gap-3 border-t border-black/10 pt-5 dark:border-white/10">
          <button
            type="button"
            onClick={actions.calculate}
            disabled={state.isCalculating}
            className="bank-secondary-btn inline-flex items-center justify-center gap-2 rounded-xl px-5 py-2.5 text-sm font-semibold disabled:opacity-60"
          >
            <Calculator className="h-4 w-4" />
            {state.isCalculating ? "Изчисляване..." : "Изчисли"}
          </button>
          <button
            type="button"
            onClick={actions.submit}
            disabled={state.isSubmitting}
            className="bank-primary-btn inline-flex items-center justify-center gap-2 rounded-xl px-6 py-2.5 text-sm font-semibold disabled:opacity-60"
          >
            <Plus className="h-4 w-4" />
            {state.isSubmitting ? "Отпускане..." : "Отпусни кредит"}
          </button>
        </div>

        {state.result ? (
          <div className="space-y-5">
            <h3 className="text-lg font-bold">Резултати</h3>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <ResultStat label="ГПР" value={formatPercent(state.result.apr)} emphasize />
              <ResultStat label="Средна месечна вноска" value={formatCurrency(state.result.averageMonthlyPayment)} />
              <ResultStat label="Общо плащания" value={formatCurrency(state.result.totalPayments)} />
              <ResultStat label="Обща лихва" value={formatCurrency(state.result.totalInterest)} />
              <ResultStat label="Общо такси" value={formatCurrency(state.result.totalFees)} />
              <ResultStat label="Общо с такси" value={formatCurrency(state.result.totalAmountWithFees)} />
            </div>
            <PaymentScheduleTable schedule={state.result.paymentSchedule} />
          </div>
        ) : null}
      </div>
    </Modal>
  );
}
