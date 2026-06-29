import { FacetedFilter, PageBody, Pagination, SearchInput } from "@/shared/components";
import { CustomerUpsertModal } from "@/pages/Customers";
import AdminUsersGrid from "./components/AdminUsersGrid";
import UserDetailsModal from "./components/UserDetailsModal";
import { useAdminUserAccessManagementPage } from "./hooks/useAdminUserAccessManagementPage";

export default function AdminUserAccessManagement() {
  const { state, actions } = useAdminUserAccessManagementPage();
  const { summary } = state;

  return (
    <PageBody>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Всички потребители</h1>
        <p className="mt-1 text-sm text-secondary">
          Предоставяйте достъп на администратори/служители, деактивирайте акаунти и отваряйте потребителски профили.
        </p>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-2">
        <FacetedFilter
          title="Роля"
          selected={state.roleFilters}
          onToggle={actions.toggleRoleFilter}
          onClear={actions.clearRoleFilters}
          options={[
            { value: "admin", label: "Администратори", count: summary.admins },
            { value: "staff", label: "Служители", count: summary.staff },
            { value: "customer", label: "Клиенти", count: summary.customers },
          ]}
        />

        <FacetedFilter
          title="Статус"
          selected={state.statusFilters}
          onToggle={actions.toggleStatusFilter}
          onClear={actions.clearStatusFilters}
          options={[
            { value: "active", label: "Активни", count: summary.active },
            { value: "inactive", label: "Неактивни", count: summary.inactive },
          ]}
        />

        <span className="bank-chip shrink-0 whitespace-nowrap rounded-full px-3 py-1.5 text-xs font-semibold">
          Потребители: {summary.totalUsers}
        </span>
      </div>

      <div className="mt-3">
        <SearchInput
          value={state.searchTerm}
          onChange={(event) => actions.setSearchTerm(event.target.value)}
          placeholder="Търсене по имейл или име"
        />
      </div>

      <AdminUsersGrid
        users={state.users}
        isLoading={state.isLoading}
        canManageAccess={state.canManageAccess}
        isUserSaving={actions.isUserSaving}
        onOpenUser={actions.openUserDetails}
        onToggleAccessOption={actions.toggleAccessOption}
        onToggleActive={actions.toggleUserActive}
      />

      {state.totalCount > 0 ? (
        <Pagination
          page={state.page}
          pageSize={state.pageSize}
          totalCount={state.totalCount}
          onPageChange={actions.goToPage}
        />
      ) : null}

      <UserDetailsModal
        user={state.selectedUser}
        onClose={actions.closeUserDetails}
        onCreateCustomer={actions.createCustomer}
      />

      <CustomerUpsertModal
        isOpen={state.linkUserContext != null}
        linkUserContext={state.linkUserContext}
        onClose={actions.closeCreateCustomer}
        onSaved={actions.handleCustomerCreated}
      />
    </PageBody>
  );
}
