import { ShieldCheck, UserRound } from "lucide-react";
import { useUserStore } from "@/stores/userStore";
import { FeatureCard } from "@/shared/components";

export default function Dashboard() {
  const user = useUserStore((state) => state.user);
  const displayName = [user.firstName, user.lastName].filter(Boolean).join(" ") || user.email;

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="mb-6">
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="mt-2 text-sm text-secondary">
          Authenticated banking workspace with role-aware navigation and shared backend contracts.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <FeatureCard title="Signed in as" value={displayName} tone="success" />
        <FeatureCard title="Auth" value="JWT + refresh" tone="info" />
        <FeatureCard title="Types" value="Generated" />
      </div>

      <div className="bank-panel mt-6 rounded-2xl p-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
          <span
            className="flex h-12 w-12 items-center justify-center rounded-2xl text-white"
            style={{ background: "var(--accent)" }}
          >
            <ShieldCheck className="h-6 w-6" />
          </span>
          <div>
            <h2 className="text-lg font-bold">Banking workspace</h2>
            <p className="mt-1 text-sm text-secondary">
              Core banking modules can build on this authenticated app structure.
            </p>
          </div>
        </div>
        <div className="mt-5 flex items-center gap-3 rounded-2xl bg-gray-100 dark:bg-white/10 p-4">
          <UserRound className="h-5 w-5" style={{ color: "var(--accent)" }} />
          <span className="text-sm font-semibold text-secondary">{user.email}</span>
        </div>
      </div>
    </section>
  );
}

