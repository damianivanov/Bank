import { useState } from "react";
import { Link } from "react-router-dom";
import { Landmark } from "lucide-react";
import logoImage from "@/assets/logo.png";
import { riseDelay } from "@/pages/Home/components/heroContent";

export default function AuthBrandPanel() {
  const [imageOk, setImageOk] = useState(true);

  return (
    <aside className="bank-hero-photo bank-rise relative hidden overflow-hidden rounded-3xl lg:flex">
      {imageOk ? (
        <img
          src="/hero.png"
          alt="Хора, които планират спокойно бъдещето си"
          className="absolute inset-0 h-full w-full object-cover"
          style={{ objectPosition: "50% 28%" }}
          loading="eager"
          decoding="async"
          onError={() => setImageOk(false)}
        />
      ) : (
        <div aria-hidden className="absolute inset-0 flex items-center justify-center">
          <Landmark className="h-16 w-16 text-accent opacity-40" />
        </div>
      )}

      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/70 via-black/25 to-black/10"
      />

      <div className="relative z-10 flex h-full w-full flex-col justify-between p-8">
        <Link to="/" className="bank-rise inline-flex items-center gap-3" style={riseDelay(100)}>
          <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-white p-[0.3rem]">
            <img src={logoImage} alt="" className="h-10 w-10 object-contain" />
          </span>
          <span>
            <span className="block text-xl font-bold tracking-tight text-white">MyBank</span>
            <span className="block text-xs font-medium text-white/70">Дигитално банкиране</span>
          </span>
        </Link>

        <div className="bank-rise" style={riseDelay(120)}>
          <p className="max-w-sm text-3xl font-bold leading-tight tracking-tight text-white">
            Вашата банка. Винаги под ръка.
          </p>
          <p className="mt-3 max-w-sm text-sm leading-6 text-white/80">
            Сигурен достъп до сметки, карти и преводи по всяко време.
          </p>
          <span aria-hidden className="mt-5 block h-px w-full bg-white/20" />
          <p className="mt-4 text-xs font-medium text-white/65">Защитена среда. Винаги под контрол.</p>
        </div>
      </div>
    </aside>
  );
}
