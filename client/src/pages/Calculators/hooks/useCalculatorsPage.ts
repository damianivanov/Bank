import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useUserStore } from "@/stores/userStore";
import { CalculatorType, type SavedCalculationDetailsModel } from "@/types";
import { useSavedCalculations } from "./useSavedCalculations";

export type CalculatorTabKey = "credit" | "leasing" | "refinancing";

export type CalculatorTab = {
  key: CalculatorTabKey;
  label: string;
  description: string;
  requiresAuth: boolean;
};

// Таб заедно с runtime lock състоянието му за текущия посетител.
export type CalculatorTabView = CalculatorTab & { locked: boolean };

const allTabs: CalculatorTab[] = [
  {
    key: "credit",
    label: "Кредит",
    description: "Месечна вноска, ГПР и пълен погасителен план.",
    requiresAuth: false,
  },
  {
    key: "leasing",
    label: "Лизинг",
    description: "Обща стойност, оскъпяване и ефективен лихвен процент.",
    requiresAuth: true,
  },
  {
    key: "refinancing",
    label: "Рефинансиране",
    description: "Сравнете текущия си кредит с нова оферта.",
    requiresAuth: true,
  },
];

const typeToTab: Record<CalculatorType, CalculatorTabKey> = {
  [CalculatorType.Credit]: "credit",
  [CalculatorType.Leasing]: "leasing",
  [CalculatorType.Refinancing]: "refinancing",
};

const tabToPath: Record<CalculatorTabKey, string> = {
  credit: "/calculators",
  leasing: "/calculators/leasing",
  refinancing: "/calculators/refinancing",
};

const paramToTab = (param: string | undefined): CalculatorTabKey =>
  param === "leasing" || param === "refinancing" ? param : "credit";

export function useCalculatorsPage() {
  const isAuthenticated = useUserStore((s) => s.isAuthenticated);
  const saved = useSavedCalculations();
  const location = useLocation();
  const navigate = useNavigate();
  const { tab: tabParam } = useParams();

  const activeTab = paramToTab(tabParam);

  // Заредена запазена калкулация, която подаваме на съответния калкулатор компонент, за да я зареди веднъж.
  const [loaded, setLoaded] = useState<SavedCalculationDetailsModel | null>(null);

  // Канонизираме непознатите / credit param URL-и (напр. /calculators/credit, /calculators/foo) към /calculators.
  useEffect(() => {
    if (tabParam && tabParam !== "leasing" && tabParam !== "refinancing") {
      navigate("/calculators", { replace: true });
    }
  }, [tabParam, navigate]);

  // Винаги показваме всички табове; ограничените се рендират заключени за анонимни посетители.
  const tabs = useMemo<CalculatorTabView[]>(
    () => allTabs.map((tab) => ({ ...tab, locked: tab.requiresAuth && !isAuthenticated })),
    [isAuthenticated],
  );

  const active = useMemo(
    () => tabs.find((tab) => tab.key === activeTab) ?? tabs[0],
    [tabs, activeTab],
  );

  const setActiveTab = useCallback(
    (key: CalculatorTabKey) => {
      navigate(tabToPath[key]);
    },
    [navigate],
  );

  const loadSaved = useCallback(
    async (id: number) => {
      const details = await saved.actions.load(id);
      if (!details) {
        return;
      }
      setLoaded(details);
      navigate(tabToPath[typeToTab[details.type]], { replace: true, state: null });
    },
    [saved.actions, navigate],
  );

  // Извиква се от калкулатор компонента, след като е консумирал заредените данни.
  const consumeLoaded = useCallback(() => setLoaded(null), []);

  // Страницата „Запазени изчисления“ ни праща тук с id за отваряне; зареждаме го веднъж,
  // после веднага чистим router state-а, за да не се задейства пак при re-render или refresh.
  const pendingLoadId = (location.state as { loadCalculationId?: number } | null)?.loadCalculationId;
  useEffect(() => {
    if (typeof pendingLoadId !== "number") {
      return;
    }
    void loadSaved(pendingLoadId);
    navigate(location.pathname, { replace: true, state: null });
  }, [pendingLoadId, loadSaved, navigate, location.pathname]);

  const state = useMemo(
    () => ({
      tabs,
      activeTab,
      active,
      isActiveLocked: active?.locked ?? false,
      isAuthenticated,
      loaded,
      saved: saved.state,
    }),
    [tabs, activeTab, active, isAuthenticated, loaded, saved.state],
  );

  const actions = useMemo(
    () => ({
      setActiveTab,
      consumeLoaded,
      saveCalculation: saved.actions.save,
      updateCalculation: saved.actions.update,
    }),
    [setActiveTab, consumeLoaded, saved.actions.save, saved.actions.update],
  );

  return { state, actions };
}
