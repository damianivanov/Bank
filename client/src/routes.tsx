import Layout from "@/components/Layout";
import AccessGate from "@/components/guards/AccessGate";
import { adminRoles, staffWorkspaceRoles } from "@/lib/access";
import { Login, Register } from "@/pages/Auth";
import { AccountDetails, AccountNew, AccountsList } from "@/pages/Accounts";
import { CreditDetails, CreditNew, CreditsList } from "@/pages/Credits";
import { CustomerDetails, CustomersList } from "@/pages/Customers";
import Home from "@/pages/Home";
import {
  AdminUserAccessManagement,
  CreditConditions,
  StaffUserManagement,
  UserCustomerCreate,
  UserDetails,
} from "@/pages/Management";
import Dashboard from "@/pages/Dashboard";
import Profile from "@/pages/Profile";
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
        path: "customers",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CustomersList />
          </AccessGate>
        ),
      },
      {
        path: "customers/:customerId",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CustomerDetails />
          </AccessGate>
        ),
      },
      {
        path: "accounts",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <AccountsList />
          </AccessGate>
        ),
      },
      {
        path: "accounts/new",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <AccountNew />
          </AccessGate>
        ),
      },
      {
        path: "accounts/:accountId",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <AccountDetails />
          </AccessGate>
        ),
      },
      {
        path: "credits",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CreditsList />
          </AccessGate>
        ),
      },
      {
        path: "credits/new",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CreditNew />
          </AccessGate>
        ),
      },
      {
        path: "credits/:creditId",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CreditDetails />
          </AccessGate>
        ),
      },
      {
        path: "management/credit-conditions",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CreditConditions />
          </AccessGate>
        ),
      },
      {
        path: "users",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <StaffUserManagement />
          </AccessGate>
        ),
      },
      {
        path: "users/:userId",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <UserDetails />
          </AccessGate>
        ),
      },
      {
        path: "users/:userId/customer",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <UserCustomerCreate />
          </AccessGate>
        ),
      },
      {
        path: "management/users/admin",
        element: (
          <AccessGate requireAuthenticated allowRoles={adminRoles}>
            <AdminUserAccessManagement />
          </AccessGate>
        ),
      },
    ],
  },
]);
