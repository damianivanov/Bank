import { Plus } from "lucide-react";
import { FacetedFilter, PageBody, Pagination, SearchInput } from "@/shared/components";
import { CustomerUpsertModal } from "@/pages/Customers";
import CounterUserCreateModal from "./components/CounterUserCreateModal";
import StaffUsersGrid from "./components/StaffUsersGrid";
import UserDetailsModal from "./components/UserDetailsModal";
import { useStaffUserManagementPage } from "./hooks/useStaffUserManagementPage";

export default function StaffUserManagement() {
  const { state, actions } = useStaffUserManagementPage();
  const { summary } = state;

  return (
    <PageBody>
      <div className="space-y-4">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Потребители</h1>
            <p className="mt-1 text-sm text-secondary">
              Всички потребители без служебни роли — със и без свързан клиент. Отворете профил, за да свържете клиент.
            </p>
          </div>
          <button type="button" onClick={actions.openCreateUser} className="bank-primary-btn shrink-0 bank-btn">
            <Plus className="h-4 w-4" />
            Нов потребител
          </button>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <FacetedFilter
            title="Клиент"
            selected={state.clientFilters}
            onToggle={actions.toggleClientFilter}
            onClear={actions.clearClientFilters}
            options={[
              { value: "linked", label: "Свързани клиенти", count: summary.linked },
              { value: "unlinked", label: "Без клиент", count: summary.missingCustomer },
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
            Потребители: {summary.total}
          </span>
        </div>

        <SearchInput
          value={state.searchTerm}
          onChange={(event) => actions.setSearchTerm(event.target.value)}
          placeholder="Търсене по имейл, име или лице "
        />

        <StaffUsersGrid users={state.users} isLoading={state.isLoading} onOpenUser={actions.openUserDetails} />

        {state.totalCount > 0 ? (
          <Pagination
            page={state.page}
            pageSize={state.pageSize}
            totalCount={state.totalCount}
            onPageChange={actions.goToPage}
          />
        ) : null}
      </div>

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

      <CounterUserCreateModal
        isOpen={state.isCreateUserOpen}
        onClose={actions.closeCreateUser}
        onCreated={actions.handleUserCreated}
      />
    </PageBody>
  );
}
