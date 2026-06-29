import { UserPlus } from "lucide-react";
import { AsyncSection, Dropdown, PageBody, PageHeader, Pagination, SearchInput } from "@/shared/components";
import { CustomerType } from "@/types";
import CustomersTable from "./components/CustomersTable";
import CustomerUpsertModal from "./components/CustomerUpsertModal";
import { useCustomersListPage } from "./hooks/useCustomersListPage";

const customerTypeFilterOptions = [
  { value: "", label: "Всички" },
  { value: String(CustomerType.Individual), label: "Физически лица" },
  { value: String(CustomerType.Company), label: "Юридически лица" },
];

export default function CustomersList() {
  const { state, actions } = useCustomersListPage();
  const hasFilter = state.appliedSearch.length > 0 || state.customerType !== undefined;

  return (
    <PageBody>
      <PageHeader
        title="Клиенти"
        subtitle="Управлявайте физически и юридически лица, включително VIP категория."
        actions={
          <button
            type="button"
            onClick={actions.openNewCustomerModal}
            className="bank-primary-btn bank-btn"
          >
            <UserPlus className="h-4 w-4" />
            Нов клиент
          </button>
        }
      />

      <div className="mt-6 flex flex-wrap items-end gap-4">
        <div className="w-52">
          <Dropdown
            label="Вид клиент"
            name="customerTypeFilter"
            value={state.customerType === undefined ? "" : String(state.customerType)}
            onChange={(event) =>
              actions.changeCustomerType(
                event.target.value === "" ? undefined : (Number(event.target.value) as CustomerType),
              )
            }
            options={customerTypeFilterOptions}
          />
        </div>

        <SearchInput
          containerClassName="max-w-sm flex-1"
          value={state.search}
          onChange={(event) => actions.changeSearch(event.target.value)}
          placeholder="Търсене по име, ЕГН, фирма или ЕИК"
        />
      </div>

      <div className="mt-4">
        <AsyncSection
          isLoading={state.isLoading}
          error={state.error}
          onRetry={actions.reload}
          isEmpty={state.customers.length === 0}
          loadingLabel="Зареждане на клиенти..."
          emptyLabel={hasFilter ? "Няма клиенти за тази заявка." : "Няма клиенти."}
        >
          <CustomersTable customers={state.customers} onEdit={actions.openEditCustomerModal} />
        </AsyncSection>

        {state.totalCount > 0 && !state.error ? (
          <Pagination
            page={state.page}
            pageSize={state.pageSize}
            totalCount={state.totalCount}
            onPageChange={actions.goToPage}
          />
        ) : null}
      </div>

      <CustomerUpsertModal
        isOpen={state.isCustomerModalOpen}
        customerId={state.editingCustomerId ?? undefined}
        onClose={actions.closeCustomerModal}
        onSaved={actions.handleCustomerSaved}
      />
    </PageBody>
  );
}
