import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useUserStore } from "@/stores/userStore";
import { hasAnyRole } from "@/lib/access";
import type { UserRole } from "@/types";

type AccessGateProps = {
  children: ReactNode;
  requireAuthenticated?: boolean;
  allowRoles?: readonly UserRole[];
  unauthenticatedRedirectTo?: string;
  unauthorizedRedirectTo?: string;
  requireUnauthenticated?: boolean;
  authenticatedRedirectTo?: string;
};

export default function AccessGate({
  children,
  requireAuthenticated = false,
  allowRoles,
  unauthenticatedRedirectTo = "/login",
  unauthorizedRedirectTo = "/dashboard",
  requireUnauthenticated = false,
  authenticatedRedirectTo = "/dashboard",
}: AccessGateProps) {
  const { user, isAuthenticated, userLoaded } = useUserStore();
  const location = useLocation();

  if (!userLoaded) {
    return (
      <div className="flex min-h-64 flex-1 items-center justify-center px-5 py-8 text-sm text-tertiary">
        Зареждане...
      </div>
    );
  }

  if (requireUnauthenticated && isAuthenticated) {
    return <Navigate to={authenticatedRedirectTo} replace />;
  }

  if (requireAuthenticated && !isAuthenticated) {
    return <Navigate to={unauthenticatedRedirectTo} replace state={{ from: location }} />;
  }

  // Принудителна смяна: автентикиран потребител с вдигнат флаг се пренасочва към екрана за смяна
  if (isAuthenticated && user.mustChangePassword && location.pathname !== "/change-password") {
    return <Navigate to="/change-password" replace />;
  }

  if (allowRoles && allowRoles.length > 0 && !hasAnyRole(user, allowRoles)) {
    return <Navigate to={unauthorizedRedirectTo} replace />;
  }

  return <>{children}</>;
}
