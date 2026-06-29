import { useEffect, useState } from "react";
import { Calculator } from "lucide-react";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { Dropdown, MoneyInputField, TextInputField } from "@/shared/components";
import {
  CalculatorType,
  PaymentType,
  type CreditCalculatorRequest,
  type CreditCalculatorResponse,
  type SaveCalculationRequest,
} from "@/types";
import { useCreditCalculator } from "../hooks/useCreditCalculator";
import CollapsibleSection from "./CollapsibleSection";
import FeeField from "./FeeField";
import PaymentScheduleTable from "./PaymentScheduleTable";
import ResultStat from "./ResultStat";
import SaveCalculationButton from "./SaveCalculationButton";

type LoadedCredit = { id: number; name: string; inputs: CreditCalculatorRequest; result: CreditCalculatorResponse };

type CreditCalculatorProps = {
  isAuthenticated: boolean;
  isSaving: boolean;
  onSave: (payload: SaveCalculationRequest) => Promise<boolean>;
  onUpdate: (id: number, payload: SaveCalculationRequest) => Promise<boolean>;
  loaded: LoadedCredit | null;
  onLoaded: () => void;
};

export default function CreditCalculator({ isAuthenticated, isSaving, onSave, onUpdate, loaded, onLoaded }: CreditCalculatorProps) {
  const { state, actions } = useCreditCalculator(isAuthenticated);
  const { hydrate } = actions;
  const [editing, setEditing] = useState<{ id: number; name: string } | null>(null);

  useEffect(() => {
    if (loaded) {
      hydrate(loaded.inputs, loaded.result);
      setEditing({ id: loaded.id, name: loaded.name });
      onLoaded();
    }
  }, [loaded, hydrate, onLoaded]);

  return (
    <div className="space-y-5">
      <div className="bank-panel rounded-2xl p-5">
        {isAuthenticated && state.products.length > 0 ? (
          <div className="mb-5 border-b border-black/5 pb-5 dark:border-white/10">
            <span className="block text-sm font-semibold">
              Кредитен продукт
            </span>
            <p className="mt-0.5 block text-xs text-tertiary">
              Изборът на продукт зарежда стандартния лихвен процент, срока и сумата от кредитните условия.
            </p>
            <div className="mt-2.5 max-w-xl">
              <Dropdown
                label="Кредитен продукт"
                hideLabel
                value={state.selectedProductId}
                onChange={(event) => actions.selectProduct(event.target.value)}
              >
                <option value="">По избор</option>
                {state.products.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name}
                  </option>
                ))}
              </Dropdown>
            </div>
          </div>
        ) : null}
        <div className="grid gap-4 md:grid-cols-2">
          <MoneyInputField
            label="Размер на кредита"
            value={state.loanAmount}
            error={state.errors.loanAmount}
            onValueChange={actions.setLoanAmount}
          />
          <TextInputField
            label="Срок (месеци)"
            type="number"
            min="1"
            step="1"
            value={state.termInMonths}
            error={state.errors.termInMonths}
            onChange={(event) => actions.setTermInMonths(event.target.value)}
          />
          <TextInputField
            label="Годишен лихвен процент (%)"
            type="number"
            min="0"
            step="0.01"
            value={state.interestRate}
            error={state.errors.interestRate}
            onChange={(event) => actions.setInterestRate(event.target.value)}
          />
          <Dropdown
            label="Погасителен план"
            value={state.paymentType}
            onChange={(event) => actions.setPaymentType(Number(event.target.value) as PaymentType)}
          >
            <option value={PaymentType.Annuity}>Анюитетен</option>
            <option value={PaymentType.Declining}>Намаляващи вноски</option>
          </Dropdown>
        </div>
      </div>

      <CollapsibleSection title="Промоционален и гратисен период" description="По избор — оставете празно, за да пропуснете">
        <div className="grid gap-4 md:grid-cols-3">
          <TextInputField
            label="Промоционален период (месеци)"
            type="number"
            min="0"
            step="1"
            value={state.promoPeriod}
            error={state.errors.promoPeriod}
            onChange={(event) => actions.setPromoPeriod(event.target.value)}
          />
          <TextInputField
            label="Промоционална лихва (%)"
            type="number"
            min="0"
            step="0.01"
            value={state.promoRate}
            error={state.errors.promoRate}
            onChange={(event) => actions.setPromoRate(event.target.value)}
          />
          <TextInputField
            label="Гратисен период (месеци)"
            type="number"
            min="0"
            step="1"
            value={state.gracePeriod}
            error={state.errors.gracePeriod}
            onChange={(event) => actions.setGracePeriod(event.target.value)}
          />
        </div>
      </CollapsibleSection>

      <CollapsibleSection title="Такси" description="По избор: еднократни, годишни и месечни такси">
        <div className="space-y-8">
          <div className="space-y-4">
            <p className="text-sm font-bold uppercase tracking-wide text-secondary">Първоначални такси</p>
            <div className="grid gap-4 md:grid-cols-2">
              <FeeField label="Такса за кандидатстване" fee={state.applicationFee} onChange={actions.setApplicationFee} />
              <FeeField label="Такса за обработка" fee={state.processingFee} onChange={actions.setProcessingFee} />
              <FeeField label="Други първоначални такси" fee={state.otherInitialFees} onChange={actions.setOtherInitialFees} />
            </div>
          </div>
          <div className="space-y-4">
            <p className="text-sm font-bold uppercase tracking-wide text-secondary">Годишни такси</p>
            <div className="grid gap-4 md:grid-cols-2">
              <FeeField label="Годишна такса за управление" fee={state.annualManagementFee} onChange={actions.setAnnualManagementFee} />
              <FeeField label="Други годишни такси" fee={state.otherAnnualFees} onChange={actions.setOtherAnnualFees} />
            </div>
          </div>
          <div className="space-y-4">
            <p className="text-sm font-bold uppercase tracking-wide text-secondary">Месечни такси</p>
            <div className="grid gap-4 md:grid-cols-2">
              <FeeField label="Месечна такса за управление" fee={state.monthlyManagementFee} onChange={actions.setMonthlyManagementFee} />
              <FeeField label="Други месечни такси" fee={state.otherMonthlyFees} onChange={actions.setOtherMonthlyFees} />
            </div>
          </div>
        </div>
      </CollapsibleSection>

      <div className="flex justify-end border-t border-black/10 pt-5 dark:border-white/10">
        <button
          type="button"
          onClick={actions.calculate}
          disabled={state.isCalculating}
          className="bank-primary-btn inline-flex items-center justify-center gap-2 rounded-xl px-6 py-2.5 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Calculator className="h-4 w-4" />
          {state.isCalculating ? "Изчисляване..." : "Изчисли"}
        </button>
      </div>

      {state.result ? (
        <div className="space-y-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-xl font-bold">Резултати</h2>
              {editing ? (
                <span className="bank-accent-pill rounded-full px-2.5 py-0.5 text-xs font-semibold">
                  Редактирате „{editing.name}“
                </span>
              ) : null}
            </div>
            {isAuthenticated ? (
              <SaveCalculationButton
                buildPayload={() => {
                  const credit = actions.buildRequest();
                  return credit ? { type: CalculatorType.Credit, credit } : null;
                }}
                isSaving={isSaving}
                onSave={onSave}
                onUpdate={onUpdate}
                editingId={editing?.id ?? null}
                editingName={editing?.name ?? ""}
                onUpdated={(name) => setEditing((prev) => (prev ? { ...prev, name } : prev))}
                onSavedAsNew={() => setEditing(null)}
              />
            ) : null}
          </div>
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
  );
}
