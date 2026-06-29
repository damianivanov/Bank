import type { ReactNode } from "react";
import AuthBrandPanel from "./AuthBrandPanel";

export default function AuthScreen({ children }: { children: ReactNode }) {
  return (
    <section className="mx-auto w-full max-w-5xl px-4 py-8 sm:py-12">
      <div className="grid items-stretch gap-6 lg:min-h-[36rem] lg:grid-cols-2">
        <AuthBrandPanel />
        <div className="mx-auto flex w-full max-w-md items-center justify-center">{children}</div>
      </div>
    </section>
  );
}
