import { Link, Navigate } from "react-router-dom";
import { ArrowRight, Landmark } from "lucide-react";
import { useUserStore } from "@/stores/userStore";

export default function Home() {
  const { userLoaded, isAuthenticated } = useUserStore();

  if (userLoaded && isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <section className="mx-auto grid min-h-[calc(100dvh-6rem)] max-w-6xl items-center gap-8 px-4 py-10 md:grid-cols-[1.08fr_0.92fr]">
      <div>
        <div className="mb-5 inline-flex items-center gap-2 rounded-full border border-emerald-200 bg-white/70 px-3 py-1 text-sm font-semibold text-emerald-800">
          <Landmark className="h-4 w-4" />
          Phase 1 foundation
        </div>
        <h1 className="max-w-3xl text-4xl font-bold tracking-tight text-slate-950 md:text-6xl">
          React, ASP.NET Core auth, and generated C# contracts.
        </h1>
        <p className="mt-5 max-w-2xl text-base leading-7 text-secondary md:text-lg">
          This first phase keeps the shared repo structure, responsive React layout, JWT authentication, users, roles, and Reinforced.Typings setup ready for later banking modules.
        </p>
        <div className="mt-7 flex flex-wrap gap-3">
          <Link to="/login" className="bank-primary-btn inline-flex items-center gap-2 rounded-xl px-5 py-3 text-sm font-semibold">
            Login
            <ArrowRight className="h-4 w-4" />
          </Link>
          <Link to="/register" className="bank-secondary-btn rounded-xl px-5 py-3 text-sm font-semibold">
            Create account
          </Link>
        </div>
      </div>
      <div className="bank-panel rounded-3xl p-5">
        <div className="grid gap-3">
          {["One Git repo", "Separate client and server folders", "Identity users and roles", "Reinforced.Typings to React"].map((item) => (
            <div key={item} className="rounded-2xl border border-slate-200 bg-white/70 p-4 text-sm font-semibold text-slate-800">
              {item}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
