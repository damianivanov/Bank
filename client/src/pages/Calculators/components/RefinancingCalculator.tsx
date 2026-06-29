import { useEffect, useState } from "react";
import { Calculator, CircleCheck, CircleX } from "lucide-react";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { MoneyInputField, TextInputField } from "@/shared/components";
import {
  CalculatorType,
  type RefinancingCalculatorRequest,
  type RefinancingCalculatorResponse,
  type SaveCalculationRequest,
} from "@/types";
import { useRefinancingCalculator } from "../hooks/useRefinancingCalculator";
import SaveCalculationButton from "./SaveCalculationButton";

type LoadedRefinancing = { id: number; name: string; inputs: RefinancingCalculatorRequest; result: RefinancingCalculatorResponse };

type RefinancingCalculatorProps = {
  isAuthenticated: boolean;
  isSaving: boolean;
  onSave: (payload: SaveCalculationRequest) => Promise<boolean>;
  onUpdate: (id: number, payload: SaveCalculationRequest) => Promise<boolean>;
  loaded: LoadedRefinancing | null;
  onLoaded: () => void;
};

export default function RefinancingCalculator({
  isAuthenticated,
  isSaving,
  onSave,
  onUpdate,
  loaded,
  onLoaded,
}: RefinancingCalculatorProps) {
  const { state, actions } = useRefinancingCalculator();
  const { hydrate } = actions;
  const { result, monthlyDelta } = state;
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
        <h3 className="text-sm font-semibold uppercase tracking-wide text-tertiary">Текущ кредит</h3>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <MoneyInputField
            label="Първоначална главница"
            value={state.principal}
            error={state.errors.principal}
            onValueChange={actions.setPrincipal}
          />
          <TextInputField
            label="Годишна лихва (%)"
            type="number"
            min="0"
            step="0.01"
            value={state.currentRate}
            error={state.errors.currentRate}
            onChange={(event) => actions.setCurrentRate(event.target.value)}
          />
          <TextInputField
            label="Срок (месеци)"
            type="number"
            min="1"
            step="1"
            value={state.currentTerm}
            error={state.errors.currentTerm}
            onChange={(event) => actions.setCurrentTerm(event.target.value)}
          />
          <TextInputField
            label="Платени вноски"
            type="number"
            min="0"
            step="1"
            value={state.paymentsMade}
            error={state.errors.paymentsMade}
            onChange={(event) => actions.setPaymentsMade(event.target.value)}
          />
          <TextInputField
            label="Такса за предсрочно погасяване (%)"
            type="number"
            min="0"
            step="0.01"
            value={state.prepaymentFee}
            error={state.errors.prepaymentFee}
            onChange={(event) => actions.setPrepaymentFee(event.target.value)}
          />
        </div>
      </div>

      <div className="bank-panel rounded-2xl p-5">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-tertiary">Нов кредит</h3>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <TextInputField
            label="Годишна лихва (%)"
            type="number"
            min="0"
            step="0.01"
            value={state.newRate}
            error={state.errors.newRate}
            onChange={(event) => actions.setNewRate(event.target.value)}
          />
          <TextInputField
            label="Такса за отпускане (%)"
            type="number"
            min="0"
            step="0.01"
            value={state.originationFeePercent}
            error={state.errors.originationFeePercent}
            onChange={(event) => actions.setOriginationFeePercent(event.target.value)}
          />
          <MoneyInputField
            label="Такса за отпускане (фиксирана)"
            value={state.originationFeeFixed}
            error={state.errors.originationFeeFixed}
            onValueChange={actions.setOriginationFeeFixed}
          />
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

      {result ? (
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
                  const refinancing = actions.buildRequest();
                  return refinancing ? { type: CalculatorType.Refinancing, refinancing } : null;
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

          <div
            className={`flex items-start gap-3 rounded-2xl border p-4 ${
              result.shouldYouSwitch
                ? "border-emerald-500/30 bg-emerald-500/10"
                : "border-rose-500/30 bg-rose-500/10"
            }`}
          >
            {result.shouldYouSwitch ? (
              <CircleCheck className="mt-0.5 h-6 w-6 shrink-0 text-emerald-500" />
            ) : (
              <CircleX className="mt-0.5 h-6 w-6 shrink-0 text-rose-500" />
            )}
            <p className="text-sm">
              {result.shouldYouSwitch
                ? "Рефинансирането изглежда изгодно — спестяванията надвишават разходите за смяна."
                : "Рефинансирането не изглежда изгодно — разходите за смяна надвишават спестяванията."}
              {" "}
              <span className="font-semibold">
                Спестявания: {formatCurrency(result.savings)} за {result.remainingMonths} оставащи месеца.
              </span>
            </p>
          </div>

          <div className="overflow-x-auto rounded-2xl border border-black/5 dark:border-white/10">
            <table className="w-full text-sm">
              <thead className="text-tertiary">
                <tr className="border-b border-black/5 text-left dark:border-white/10">
                  <th className="px-4 py-3 font-semibold">Показател</th>
                  <th className="px-4 py-3 text-right font-semibold">Текущ</th>
                  <th className="px-4 py-3 text-right font-semibold">Нов</th>
                  <th className="px-4 py-3 text-right font-semibold">Разлика</th>
                </tr>
              </thead>
              <tbody>
                <tr className="border-b border-black/5 dark:border-white/5">
                  <td className="px-4 py-3">Годишна лихва</td>
                  <td className="px-4 py-3 text-right">{formatPercent(result.current.annualRatePercent)}</td>
                  <td className="px-4 py-3 text-right">{formatPercent(result.new.annualRatePercent)}</td>
                  <td className="px-4 py-3 text-right text-tertiary">—</td>
                </tr>
                <tr className="border-b border-black/5 dark:border-white/5">
                  <td className="px-4 py-3">Срок (месеци)</td>
                  <td className="px-4 py-3 text-right">{result.current.termMonths}</td>
                  <td className="px-4 py-3 text-right">{result.new.termMonths}</td>
                  <td className="px-4 py-3 text-right text-tertiary">—</td>
                </tr>
                <tr className="border-b border-black/5 dark:border-white/5">
                  <td className="px-4 py-3">Такси</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(result.current.fees)}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(result.new.fees)}</td>
                  <td className="px-4 py-3 text-right text-tertiary">—</td>
                </tr>
                <tr className="border-b border-black/5 dark:border-white/5">
                  <td className="px-4 py-3 font-semibold">Месечна вноска</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(result.current.monthlyPayment)}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(result.new.monthlyPayment)}</td>
                  <td className={`px-4 py-3 text-right font-semibold ${monthlyDelta >= 0 ? "text-emerald-500" : "text-rose-500"}`}>
                    {formatCurrency(monthlyDelta)}
                  </td>
                </tr>
                <tr>
                  <td className="px-4 py-3 font-bold">Общо за плащане</td>
                  <td className="px-4 py-3 text-right font-bold">{formatCurrency(result.current.totalToPay)}</td>
                  <td className="px-4 py-3 text-right font-bold">{formatCurrency(result.new.totalToPay)}</td>
                  <td className={`px-4 py-3 text-right font-bold ${result.shouldYouSwitch ? "text-emerald-500" : "text-rose-500"}`}>
                    {formatCurrency(result.savings)}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </div>
  );
}
