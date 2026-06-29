import { Lock } from "lucide-react";
import { Link, useLocation } from "react-router-dom";

type LockedCalculatorPanelProps = {
  label: string;
};

// Показва се вместо ограничен калкулатор за анонимни посетители. Формата на калкулатора никога не се
// монтира, така че заключеният URL не разкрива нищо — след вход потребителят се връща на същия този таб.
export default function LockedCalculatorPanel({ label }: LockedCalculatorPanelProps) {
  const location = useLocation();

  return (
    <div className="bank-panel flex flex-col items-center rounded-3xl px-6 py-14 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-black/5 text-tertiary dark:bg-white/5">
        <Lock className="h-7 w-7" />
      </span>
      <h2 className="mt-5 text-xl font-bold tracking-tight">Калкулаторът „{label}“ е заключен</h2>
      <p className="mt-2 max-w-md text-sm text-secondary">
        Влезте, за да отключите калкулатора „{label}“ и да запазвате изчисленията си в профила.
      </p>
      <Link
        to="/login"
        state={{ from: location }}
        className="bank-primary-btn mt-6 inline-flex items-center gap-1.5 rounded-xl px-5 py-2.5 text-sm font-semibold"
      >
        <Lock className="h-4 w-4" />
        Влезте, за да отключите
      </Link>
    </div>
  );
}
