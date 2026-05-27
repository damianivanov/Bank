import { Navigate } from "react-router-dom";
import { isAdmin } from "@/lib/access";
import { useUserStore } from "@/stores/userStore";

export default function UserAccessManagementPage() {
  const currentUser = useUserStore((state) => state.user);
  const destinationPath = isAdmin(currentUser) ? "/management/users/admin" : "/users";

  return <Navigate to={destinationPath} replace />;
}
