import Layout from "@/components/Layout";
import AccessGate from "@/components/guards/AccessGate";
import { Login, Register } from "@/pages/Auth";
import Home from "@/pages/Home";
import Dashboard from "@/pages/Dashboard";
import Profile from "@/pages/Profile";
import { UserRole } from "@/types";
import { createBrowserRouter } from "react-router-dom";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Home />,
      },
      {
        path: "login",
        element: <Login />,
      },
      {
        path: "register",
        element: <Register />,
      },
      {
        path: "dashboard",
        element: (
          <AccessGate requireAuthenticated>
            <Dashboard />
          </AccessGate>
        ),
      },
      {
        path: "profile",
        element: (
          <AccessGate requireAuthenticated>
            <Profile />
          </AccessGate>
        ),
      },
      {
        path: "management",
        element: (
          <AccessGate requireAuthenticated allowRoles={[UserRole.Admin]}>
            <Dashboard />
          </AccessGate>
        ),
      },
    ],
  },
]);
