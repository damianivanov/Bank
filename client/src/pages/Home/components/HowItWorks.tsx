import { Link } from "react-router-dom";
import { Calculator } from "lucide-react";
import { howSteps, riseDelay } from "./heroContent";

export default function HowItWorks() {
  return (
    <section className="px-4 pb-16">
      <div className="bank-panel rounded-3xl p-6 md:p-10">
        <div className="mx-auto mb-10 max-w-2xl text-center">
          <h2 className="text-2xl font-bold tracking-tight md:text-3xl">Как започвате</h2>
          <p className="mt-2 text-base leading-7 text-secondary">Три стъпки от регистрация до пълен контрол.</p>
        </div>

        <ol className="flex flex-col gap-10 md:flex-row md:gap-0">
          {howSteps.map(({ icon: Icon, title, description }, index) => {
            const isLast = index === howSteps.length - 1;

            return (
              <li
                key={title}
                className="bank-rise relative flex flex-1 flex-col items-center text-center"
                style={riseDelay(index * 80)}
              >
                {!isLast ? (
                  <span
                    aria-hidden
                    className="absolute left-1/2 top-6 hidden h-px w-full bg-[var(--accent-border)] md:block"
                  />
                ) : null}

                <span className="relative z-10 flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-[var(--accent)] text-white">
                  <Icon className="h-5 w-5" />
                </span>

                <div className="mt-5 md:px-6">
                  <span className="text-xs font-bold uppercase tracking-wide text-tertiary">Стъпка 0{index + 1}</span>
                  <h3 className="mt-1 text-lg font-semibold">{title}</h3>
                  <p className="mt-1 text-sm leading-6 text-secondary">{description}</p>
                </div>
              </li>
            );
          })}
        </ol>

        <div className="mt-10 border-t border-black/5 pt-8 text-center dark:border-white/10">
          <p className="w-full text-base leading-7 text-secondary">
            Пресметнете месечните си вноски за секунди с нашите калкулатори.
          </p>
          <div className="mt-7 flex flex-wrap justify-center gap-3">
            <Link
              to="/calculators"
              className="bank-primary-btn inline-flex items-center gap-2 rounded-xl px-5 py-3 text-sm font-semibold"
            >
              <Calculator className="h-4 w-4" />
              Към калкулаторите
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
