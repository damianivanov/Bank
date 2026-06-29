import { useEffect, useState } from "react";
import { Calculator } from "lucide-react";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { MoneyInputField, TextInputField } from "@/shared/components";
import {
  CalculatorType,
  type LeasingCalculatorRequest,
  type LeasingCalculatorResponse,
  type SaveCalculationRequest,
} from "@/types";
import { useLeasingCalculator } from "../hooks/useLeasingCalculator";
import FeeField from "./FeeField";
import ResultStat from "./ResultStat";
import SaveCalculationButton from "./SaveCalculationButton";

type LoadedLeasing = { id: number; name: string; inputs: LeasingCalculatorRequest; result: LeasingCalculatorResponse };

type LeasingCalculatorProps = {
  isAuthenticated: boolean;
  isSaving: boolean;
  onSave: (payload: SaveCalculationRequest) => Promise<boolean>;
  onUpdate: (id: number, payload: SaveCalculationRequest) => Promise<boolean>;
  loaded: LoadedLeasing | null;
  onLoaded: () => void;
};

export default function LeasingCalculator({ isAuthenticated, isSaving, onSave, onUpdate, loaded, onLoaded }: LeasingCalculatorProps) {
  const { state, actions } = useLeasingCalculator();
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
        <div className="grid gap-4 md:grid-cols-2">
          <MoneyInputField
            label="Цена на актива (с ДДС)"
            value={state.priceWithVAT}
            error={state.errors.priceWithVAT}
            onValueChange={actions.setPriceWithVAT}
          />
          <MoneyInputField
            label="Първоначална вноска"
            value={state.downPayment}
            error={state.errors.downPayment}
            onValueChange={actions.setDownPayment}
          />
          <TextInputField
            label="Срок на лизинг (месеци)"
            type="number"
            min="1"
            step="1"
            value={state.leasingTerm}
            error={state.errors.leasingTerm}
            onChange={(event) => actions.setLeasingTerm(event.target.value)}
          />
          <MoneyInputField
            label="Месечна вноска"
            value={state.monthlyPayment}
            error={state.errors.monthlyPayment}
            onValueChange={actions.setMonthlyPayment}
          />
          <div className="md:col-span-2">
            <FeeField label="Такса за обработка (по избор)" fee={state.processingFee} onChange={actions.setProcessingFee} />
          </div>
        </div>
      </div>

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
                  const leasing = actions.buildRequest();
                  return leasing ? { type: CalculatorType.Leasing, leasing } : null;
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
            <ResultStat label="Ефективен лихвен процент" value={formatPercent(state.result.effectiveInterestRate)} emphasize />
            <ResultStat label="Общо платено" value={formatCurrency(state.result.totalPaid)} />
            <ResultStat label="Общо оскъпяване" value={formatCurrency(state.result.totalMarkup)} />
            <ResultStat label="Оскъпяване" value={formatPercent(state.result.markupPercentage)} />
            <ResultStat label="Такса за обработка" value={formatCurrency(state.result.processingFeeAmount)} />
            <ResultStat label="Цена на актива" value={formatCurrency(state.result.totalCost)} />
          </div>
        </div>
      ) : null}
    </div>
  );
}
