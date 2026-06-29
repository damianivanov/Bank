import type { CSSProperties } from "react";
import {
  Smartphone,
  UserPlus,
  Wallet,
  type LucideIcon,
} from "lucide-react";

export const riseDelay = (ms: number): CSSProperties => ({ "--rise-delay": `${ms}ms` }) as CSSProperties;

export const heroHighlights = ["Регистрация за минути", "Ролеви достъп", "Защитени сесии"];

export type HowStep = {
  icon: LucideIcon;
  title: string;
  description: string;
};

export const howSteps: HowStep[] = [
  {
    icon: UserPlus,
    title: "Създайте профил",
    description: "Регистрирайте се онлайн за няколко минути — нужен е само имейл.",
  },
  {
    icon: Wallet,
    title: "Съберете банкирането си",
    description: "Сметки, кредити и операции, събрани на едно подредено място.",
  },
  {
    icon: Smartphone,
    title: "Управлявайте отвсякъде",
    description: "Следете и действайте от телефон или компютър, когато пожелаете.",
  },
];

