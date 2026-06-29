import { Link } from "react-router-dom";
import { ArrowRight, Check, Sparkles } from "lucide-react";
import HeroShowcase from "./HeroShowcase";
import { heroHighlights, riseDelay } from "./heroContent";

export default function HeroSection() {
  return (
    <section className="relative overflow-hidden px-4 pb-16 pt-8 md:pt-12">
      <div aria-hidden className="pointer-events-none absolute inset-0 z-0 overflow-hidden">
        <span className="bank-aurora-orb bank-aurora-orb-accent" />
        <span className="bank-aurora-orb bank-aurora-orb-blue" />
      </div>

      <div className="relative grid items-center gap-10 md:grid-cols-[1.08fr_0.92fr] md:gap-8">
        <div>
          <div
            className="bank-rise bank-accent-pill mb-5 inline-flex items-center gap-2 rounded-full px-3 py-1 text-sm font-semibold"
            style={riseDelay(0)}
          >
            <Sparkles className="h-4 w-4" />
            Дигитално банкиране
          </div>

          <h1 className="bank-rise max-w-2xl text-4xl font-bold leading-[1.08] tracking-tight md:text-6xl" style={riseDelay(60)}>
            Вашите финанси — ясни, сигурни и винаги под ръка.
          </h1>

          <p className="bank-rise mt-5 max-w-xl text-base leading-7 text-secondary md:text-lg" style={riseDelay(120)}>
            Открийте сметка за минути, следете кредитите си и пресмятайте вноски онлайн. А екипите
            управляват всичко това в сигурна работна среда с роли и права.
          </p>

          <div className="bank-rise mt-7 flex flex-wrap gap-3" style={riseDelay(180)}>
            <Link
              to="/register"
              className="bank-primary-btn inline-flex items-center gap-2 rounded-xl px-5 py-3 text-sm font-semibold"
            >
              Създайте профил
              <ArrowRight className="h-4 w-4" />
            </Link>
            <Link to="/login" className="bank-secondary-btn rounded-xl px-5 py-3 text-sm font-semibold">
              Вход за служители
            </Link>
          </div>

          <ul className="bank-rise mt-9 flex flex-wrap gap-x-6 gap-y-2.5" style={riseDelay(240)}>
            {heroHighlights.map((item) => (
              <li key={item} className="flex items-center gap-2 text-sm font-medium text-secondary">
                <Check className="h-4 w-4 shrink-0 text-accent" />
                {item}
              </li>
            ))}
          </ul>
        </div>

        <HeroShowcase />
      </div>
    </section>
  );
}
