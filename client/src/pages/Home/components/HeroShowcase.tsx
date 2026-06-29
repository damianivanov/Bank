import { useState } from "react";
import { Landmark } from "lucide-react";
import { riseDelay } from "./heroContent";

export default function HeroShowcase() {
  const [imageOk, setImageOk] = useState(true);

  return (
    <div className="bank-rise relative" style={riseDelay(120)}>
      <div className="bank-hero-photo aspect-[16/11] rounded-3xl md:aspect-[5/6]">
        {imageOk ? (
          <img
            src="/hero.png"
            alt="Хора, които планират спокойно бъдещето си"
            className="h-full w-full object-cover"
            loading="eager"
            decoding="async"
            fetchPriority="high"
            onError={() => setImageOk(false)}
          />
        ) : (
          <div aria-hidden className="flex h-full w-full items-center justify-center">
            <Landmark className="h-16 w-16 text-accent opacity-40" />
          </div>
        )}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/15 via-transparent to-transparent"
        />
      </div>
    </div>
  );
}
