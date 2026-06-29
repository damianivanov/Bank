import Layout from "@/components/Layout";
import AccessGate from "@/components/guards/AccessGate";
import { adminRoles, customerRoles, staffWorkspaceRoles } from "@/lib/access";
import { ForcePasswordChange, Login, Register } from "@/pages/Auth";
import { AccountsList } from "@/pages/Accounts";
import { Calculators, SavedCalculations } from "@/pages/Calculators";
import { CreditDetails, CreditsList } from "@/pages/Credits";
import { CustomerDetails, CustomersList } from "@/pages/Customers";
import { MyBanking, MyCreditDetails } from "@/pages/SelfService";
import Home from "@/pages/Home";
import {
  AdminUserAccessManagement,
  CreditConditions,
  DepositApprovals,
  Errors,
  StaffUserManagement,
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
        element: (
          <AccessGate requireUnauthenticated>
            <Home />
          </AccessGate>
        ),
      },
      {
        path: "login",
        element: (
          <AccessGate requireUnauthenticated>
            <Login />
          </AccessGate>
        ),
      },
      {
        path: "register",
        element: (
          <AccessGate requireUnauthenticated>
            <Register />
          </AccessGate>
        ),
      },
      {
        // Кредитният калкулатор е на /calculators; заключените калкулатори са /calculators/leasing и
        // /calculators/refinancing. Статичният маршрут /calculators/saved по-долу има приоритет пред :tab?.
        // Bare AccessGate (без requireAuthenticated) пуска анонимни посетители към публичния калкулатор,
        // но изпълнява пренасочването при принудителна смяна на парола — иначе authenticated потребител с
        // вдигнат флаг зарежда страницата и получава 403 от backend-а вместо екрана за смяна.
        path: "calculators/:tab?",
        element: (
          <AccessGate>
            <Calculators />
          </AccessGate>
        ),
      },
      {
        path: "calculators/saved",
        element: (
          <AccessGate requireAuthenticated>
            <SavedCalculations />
          </AccessGate>
        ),
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
        path: "change-password",
        element: (
          <AccessGate requireAuthenticated>
            <ForcePasswordChange />
          </AccessGate>
        ),
      },
      {
        path: "my-banking",
        element: (
          <AccessGate requireAuthenticated allowRoles={customerRoles}>
            <MyBanking />
          </AccessGate>
        ),
      },
      {
        path: "my-banking/credits/:creditId",
        element: (
          <AccessGate requireAuthenticated allowRoles={customerRoles}>
            <MyCreditDetails />
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
        path: "credits",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <CreditsList />
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
        path: "management/deposit-approvals",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <DepositApprovals />
          </AccessGate>
        ),
      },
      {
        path: "all-users",
        element: (
          <AccessGate requireAuthenticated allowRoles={staffWorkspaceRoles}>
            <StaffUserManagement />
          </AccessGate>
        ),
      },
      {
        path: "management/users",
        element: (
          <AccessGate requireAuthenticated allowRoles={adminRoles}>
            <AdminUserAccessManagement />
          </AccessGate>
        ),
      },
      {
        path: "management/errors",
        element: (
          <AccessGate requireAuthenticated allowRoles={adminRoles}>
            <Errors />
          </AccessGate>
        ),
      },
    ],
  },
]);
