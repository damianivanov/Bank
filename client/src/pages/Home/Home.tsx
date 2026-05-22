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
        <div className="bank-accent-pill mb-5 inline-flex items-center gap-2 rounded-full px-3 py-1 text-sm font-semibold">
          <Landmark className="h-4 w-4" />
          Bank operations
        </div>
        <h1 className="max-w-3xl text-4xl font-bold tracking-tight text-slate-950 md:text-6xl">
          Banking operations workspace.
        </h1>
        <p className="mt-5 max-w-2xl text-base leading-7 text-secondary md:text-lg">
          A responsive workspace for authenticated banking workflows, role-aware navigation, and shared C# contracts in React.
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
          {["Accounts", "Customers", "Transactions", "Administration"].map((item) => (
            <div key={item} className="rounded-2xl border border-slate-200 bg-white/70 p-4 text-sm font-semibold text-slate-800">
              {item}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
