import { Outlet } from "react-router-dom";
import { useUserStore } from "@/stores/userStore";
import { RateLimitModal } from "@/shared/components";
import Sidebar from "./Sidebar";

export default function Layout() {
  const { userLoaded, isAuthenticated } = useUserStore();
  const isReadyAuthenticated = userLoaded && isAuthenticated;

  if (isReadyAuthenticated) {
    return (
      <div className="flex min-h-dvh flex-col md:flex-row md:overflow-hidden">
        <Sidebar />
        <main className="min-h-0 min-w-0 flex-1 overflow-y-auto pb-20 md:h-dvh md:pb-0">
          <Outlet />
        </main>
        <RateLimitModal />
      </div>
    );
  }

  return (
    <div className="min-h-dvh">
      <Sidebar />
      <main className="min-w-0">
        <div className="mx-auto w-full lg:w-[70%]">
          <Outlet />
        </div>
      </main>
      <RateLimitModal />
    </div>
  );
}
