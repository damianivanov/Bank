import { Link } from "react-router-dom";
import { Clock, LogIn } from "lucide-react";
import Modal from "./Modal";
import { useRateLimitStore } from "@/stores/rateLimitStore";

// Показва се, когато гост изчерпа часовия лимит на калкулатора. Логнатите потребители нямат лимит,
// затова поканата за вход е смисленото действие тук.
export default function RateLimitModal() {
  const isOpen = useRateLimitStore((s) => s.isOpen);
  const close = useRateLimitStore((s) => s.close);

  return (
    <Modal isOpen={isOpen} onClose={close} title="Достигнахте лимита за гости">
      <div className="flex items-center gap-3">
        <span className="bank-icon-tile-soft flex h-10 w-10 shrink-0 items-center justify-center rounded-full">
          <Clock className="h-5 w-5" />
        </span>
        <p className="text-sm font-semibold">Като гост можете да правите ограничен брой изчисления на час.</p>
      </div>

      <p className="mt-3 text-sm text-secondary">
        Влезте в профила си, за да изчислявате без ограничения, да отключите калкулаторите Лизинг и
        Рефинансиране и да запазвате резултатите си. Или опитайте отново малко по-късно.
      </p>

      <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-xs text-secondary">
          Нямате профил?{" "}
          <Link to="/register" onClick={close} className="bank-accent-link font-semibold ms-1 hover:underline!">
            Регистрирайте се
          </Link>
        </p>
        <Link
          to="/login"
          onClick={close}
          className="bank-primary-btn inline-flex items-center justify-center gap-1.5 bank-btn"
        >
          <LogIn className="h-4 w-4" />
          Вход
        </Link>
      </div>
    </Modal>
  );
}
