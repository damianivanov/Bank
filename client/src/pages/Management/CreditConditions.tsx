import { Pencil } from "lucide-react";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { AsyncSection, EntityGrid, PageBody } from "@/shared/components";
import { useCreditConditionsPage } from "./hooks/useCreditConditionsPage";
import CreditConditionEditModal from "./components/CreditConditionEditModal";

export default function CreditConditions() {
  const { state, actions } = useCreditConditionsPage();
  const { canEdit } = state;

  return (
    <PageBody>
      <div className="mx-auto w-full max-w-7xl">
        <h1 className="text-2xl font-bold tracking-tight md:text-3xl">Кредитни условия</h1>
        <p className="mt-0.5 text-sm text-secondary">
          {canEdit
            ? "Стандартни и VIP условия за наличните кредитни продукти. Промените важат за нови кредити."
            : "Стандартни и VIP условия за наличните кредитни продукти (само за преглед)."}
        </p>

        <div className="mt-4">
          <AsyncSection
            isLoading={state.isLoading}
            error={state.error}
            onRetry={actions.reload}
            loadingLabel="Зареждане на кредитните условия..."
          >
            <EntityGrid>
              <thead className="text-xs">
                <tr className="border-b border-slate-200 uppercase tracking-wide text-tertiary">
                  <th className="px-3 py-2.5">Вид</th>
                  <th className="px-3 py-2.5">Стандартен процент</th>
                  <th className="px-3 py-2.5">VIP процент</th>
                  <th className="px-3 py-2.5">Макс. сума</th>
                  <th className="px-3 py-2.5">Макс. срок</th>
                  <th className="px-3 py-2.5">Стандартна такса</th>
                  <th className="px-3 py-2.5">VIP такса</th>
                  {canEdit ? <th className="px-3 py-2.5 text-right">Действия</th> : null}
                </tr>
              </thead>
              <tbody>
                {state.conditions.map((condition) => (
                  <tr key={condition.id} className="border-b border-slate-100 text-sm last:border-b-0">
                    <td className="px-3 py-2.5 font-semibold">{condition.name}</td>
                    <td className="px-3 py-2.5">{formatPercent(condition.standardAnnualInterestRate)}</td>
                    <td className="px-3 py-2.5">{formatPercent(condition.vipAnnualInterestRate)}</td>
                    <td className="px-3 py-2.5">{formatCurrency(condition.maximumAmount)}</td>
                    <td className="px-3 py-2.5">{condition.maximumTermMonths} месеца</td>
                    <td className="px-3 py-2.5">{formatCurrency(condition.standardGrantingFee)}</td>
                    <td className="px-3 py-2.5">{formatCurrency(condition.vipGrantingFee)}</td>
                    {canEdit ? (
                      <td className="px-3 py-2.5 text-right">
                        <button
                          type="button"
                          onClick={() => actions.startEdit(condition)}
                          className="bank-secondary-btn inline-flex h-8 w-8 items-center justify-center rounded-lg"
                          aria-label={`Редактирай ${condition.name}`}
                        >
                          <Pencil className="h-4 w-4" />
                        </button>
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </EntityGrid>
            <p className="mt-3 text-xs text-tertiary">
              Посочените условия са препоръчителни.
            </p>
          </AsyncSection>
        </div>
      </div>

      {state.editingCondition ? (
        <CreditConditionEditModal
          condition={state.editingCondition}
          onClose={actions.closeEdit}
          onSaved={actions.reload}
        />
      ) : null}
    </PageBody>
  );
}
