import { Bookmark, Lock } from "lucide-react";
import { Link } from "react-router-dom";
import { PageBody } from "@/shared/components";
import CreditCalculator from "./components/CreditCalculator";
import LeasingCalculator from "./components/LeasingCalculator";
import LockedCalculatorPanel from "./components/LockedCalculatorPanel";
import RefinancingCalculator from "./components/RefinancingCalculator";
import { useCalculatorsPage } from "./hooks/useCalculatorsPage";

export default function Calculators() {
  const { state, actions } = useCalculatorsPage();
  const { loaded } = state;

  const loadedCredit =
    loaded && loaded.creditInputs && loaded.creditResult
      ? { id: loaded.id, name: loaded.name, inputs: loaded.creditInputs, result: loaded.creditResult }
      : null;
  const loadedLeasing =
    loaded && loaded.leasingInputs && loaded.leasingResult
      ? { id: loaded.id, name: loaded.name, inputs: loaded.leasingInputs, result: loaded.leasingResult }
      : null;
  const loadedRefinancing =
    loaded && loaded.refinancingInputs && loaded.refinancingResult
      ? { id: loaded.id, name: loaded.name, inputs: loaded.refinancingInputs, result: loaded.refinancingResult }
      : null;

  return (
    <PageBody>
      <div className="mb-5 flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-bold tracking-tight">Калкулатори</h1>
        <div className="inline-flex flex-wrap gap-1 rounded-2xl bank-panel p-1">
          {state.tabs.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => actions.setActiveTab(tab.key)}
              aria-current={tab.key === state.activeTab ? "page" : undefined}
              className={
                tab.key === state.activeTab
                  ? "bank-primary-btn inline-flex items-center gap-1.5 bank-btn"
                  : "inline-flex items-center gap-1.5 bank-btn text-secondary transition hover:bg-black/5 dark:hover:bg-white/5"
              }
            >
              {tab.label}
              {tab.locked ? <Lock className="h-3.5 w-3.5 opacity-70" /> : null}
            </button>
          ))}
        </div>

        {state.isAuthenticated ? (
          <Link
            to="/calculators/saved"
            className="bank-secondary-btn inline-flex items-center gap-1.5 bank-btn sm:ml-auto"
          >
            <Bookmark className="h-4 w-4" />
            Запазени изчисления
          </Link>
        ) : null}
      </div>

      {!state.isAuthenticated && !state.isActiveLocked ? (
        <div className="mb-5 flex items-start gap-2 rounded-2xl border border-black/5 bg-black/[0.02] px-4 py-3 text-sm text-secondary dark:border-white/10 dark:bg-white/[0.03]">
          <Lock className="mt-0.5 h-4 w-4 shrink-0 text-tertiary" />
          <p>
            <Link to="/login" className="bank-accent-link font-semibold">
              Влезте
            </Link>{" "}
            за да отключите калкулаторите Лизинг и Рефинансиране и да запазвате изчисленията си.
          </p>
        </div>
      ) : null}

      {state.isActiveLocked ? (
        <LockedCalculatorPanel label={state.active.label} />
      ) : state.activeTab === "credit" ? (
        <CreditCalculator
          isAuthenticated={state.isAuthenticated}
          isSaving={state.saved.isSaving}
          onSave={actions.saveCalculation}
          onUpdate={actions.updateCalculation}
          loaded={loadedCredit}
          onLoaded={actions.consumeLoaded}
        />
      ) : state.activeTab === "leasing" ? (
        <LeasingCalculator
          isAuthenticated={state.isAuthenticated}
          isSaving={state.saved.isSaving}
          onSave={actions.saveCalculation}
          onUpdate={actions.updateCalculation}
          loaded={loadedLeasing}
          onLoaded={actions.consumeLoaded}
        />
      ) : state.activeTab === "refinancing" ? (
        <RefinancingCalculator
          isAuthenticated={state.isAuthenticated}
          isSaving={state.saved.isSaving}
          onSave={actions.saveCalculation}
          onUpdate={actions.updateCalculation}
          loaded={loadedRefinancing}
          onLoaded={actions.consumeLoaded}
        />
      ) : null}
    </PageBody>
  );
}
