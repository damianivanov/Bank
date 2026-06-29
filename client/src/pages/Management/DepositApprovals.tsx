import { Dropdown, PageBody, Pagination, SearchInput } from "@/shared/components";
import { DepositRequestStatus } from "@/types";
import ApproveDepositModal from "./components/ApproveDepositModal";
import DepositApprovalsGrid from "./components/DepositApprovalsGrid";
import RejectDepositModal from "./components/RejectDepositModal";
import { useDepositApprovalsPage } from "./hooks/useDepositApprovalsPage";

const statusFilterOptions = [
  { value: "", label: "Всички" },
  { value: String(DepositRequestStatus.Pending), label: "Чакащи" },
  { value: String(DepositRequestStatus.Approved), label: "Одобрени" },
  { value: String(DepositRequestStatus.Rejected), label: "Отхвърлени" },
];

export default function DepositApprovals() {
  const { state, actions } = useDepositApprovalsPage();

  return (
    <PageBody>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Заявки за депозит</h1>
        <p className="mt-1 text-sm text-secondary">
          Прегледайте и одобрете или отхвърлете заявки за депозит. Салдото се кредитира едва при одобрение.
        </p>
      </div>

      <div className="mt-4 flex flex-wrap items-end gap-4">
        <div className="w-48">
          <Dropdown
            label="Статус"
            name="statusFilter"
            value={state.statusFilter === undefined ? "" : String(state.statusFilter)}
            onChange={(event) =>
              actions.setStatusFilter(
                event.target.value === "" ? undefined : (Number(event.target.value) as DepositRequestStatus),
              )
            }
            options={statusFilterOptions}
          />
        </div>

        <SearchInput
          containerClassName="max-w-sm flex-1"
          value={state.search}
          onChange={(event) => actions.changeSearch(event.target.value)}
          placeholder="Търсене по IBAN или клиент"
        />
      </div>

      <div className="mt-4">
        {state.isLoading ? (
          <p className="text-sm text-secondary">Зареждане на заявките...</p>
        ) : (
          <DepositApprovalsGrid
            requests={state.requests}
            processingId={state.processingId}
            onApprove={actions.openApprove}
            onReject={actions.openReject}
          />
        )}
      </div>

      {state.totalCount > 0 ? (
        <Pagination
          page={state.page}
          pageSize={state.pageSize}
          totalCount={state.totalCount}
          onPageChange={actions.goToPage}
        />
      ) : null}

      <RejectDepositModal
        request={state.rejectTarget}
        isSubmitting={state.processingId !== null && state.rejectTarget?.id === state.processingId}
        onClose={actions.closeReject}
        onConfirm={actions.confirmReject}
      />

      <ApproveDepositModal
        request={state.approveTarget}
        isSubmitting={state.processingId !== null && state.approveTarget?.id === state.processingId}
        onClose={actions.closeApprove}
        onConfirm={actions.confirmApprove}
      />
    </PageBody>
  );
}
