import { Link } from "react-router-dom";
import { ArrowUpRight } from "lucide-react";
import { PageBody } from "@/shared/components";
import { navSections } from "@/components/navigation";
import { hasAnyRole } from "@/lib/access";
import { useUserStore } from "@/stores/userStore";
import { getGreeting, quickActionDescriptions } from "./dashboardContent";

export default function Dashboard() {
  const user = useUserStore((state) => state.user);
  const firstName = user.firstName?.trim() || user.email.split("@")[0] || "потребител";
  const greeting = getGreeting(new Date().getHours());

  const quickActions = navSections
    .flatMap((section) => section.items)
    .filter((item) => item.to !== "/dashboard")
    .filter((item) => !item.allowedRoles || hasAnyRole(user, item.allowedRoles));

  return (
    <PageBody>
      <header className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight">
          {greeting}, {firstName}.
        </h1>
        <p className="mt-2 text-sm text-secondary">Ето какво можете да направите оттук.</p>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {quickActions.map((item) => {
          const Icon = item.icon;

          return (
            <Link key={item.to} to={item.to} className="bank-feature-card group block rounded-2xl p-5">
              <div className="flex items-center justify-between">
                <span className="bank-icon-tile-soft flex h-11 w-11 items-center justify-center rounded-xl">
                  <Icon className="h-5 w-5" />
                </span>
                <ArrowUpRight className="h-4 w-4 text-accent transition-transform duration-200 group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
              </div>
              <h2 className="mt-4 text-base font-semibold">{item.label}</h2>
              <p className="mt-1.5 text-sm leading-6 text-secondary">{quickActionDescriptions[item.to] ?? ""}</p>
            </Link>
          );
        })}
      </div>
    </PageBody>
  );
}
