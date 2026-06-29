import { ArrowLeft, Bookmark } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { PageBody } from "@/shared/components";
import SavedCalculationsPanel from "./components/SavedCalculationsPanel";
import { useSavedCalculations } from "./hooks/useSavedCalculations";

export default function SavedCalculations() {
  const { state, actions } = useSavedCalculations();
  const navigate = useNavigate();

  // Зареждането на запазена калкулация се случва на Calculators страницата (тя владее формите
  // на калкулаторите), затова подаваме id-то през router state и оставяме нея да зареди правилния таб.
  const handleLoad = (id: number) => {
    navigate("/calculators", { state: { loadCalculationId: id } });
  };

  return (
    <PageBody>
      <div className="mb-6 flex items-start gap-3">
        <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-[var(--accent)] text-white">
          <Bookmark className="h-6 w-6" />
        </span>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Запазени изчисления</h1>
          <p className="mt-1 text-sm text-secondary">
            Отворете запазено изчисление в калкулаторите или премахнете тези, които вече не са ви нужни.
          </p>
        </div>
      </div>

      <Link
        to="/calculators"
        className="bank-accent-link mb-5 inline-flex items-center gap-1.5 text-sm font-semibold"
      >
        <ArrowLeft className="h-4 w-4" />
        Назад към калкулаторите
      </Link>

      <SavedCalculationsPanel
        items={state.items}
        isLoading={state.isLoading}
        loadingId={null}
        deletingId={state.deletingId}
        onLoad={handleLoad}
        onDelete={actions.remove}
      />
    </PageBody>
  );
}
