import { PageBody, Pagination, SearchInput } from "@/shared/components";
import ErrorDetailsModal from "./components/ErrorDetailsModal";
import ErrorsGrid from "./components/ErrorsGrid";
import { useErrorsPage } from "./hooks/useErrorsPage";

export default function Errors() {
  const { state, actions } = useErrorsPage();

  return (
    <PageBody>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Грешки</h1>
        <p className="mt-1 text-sm text-secondary">
          Логнати сървърни грешки от API заявки. Само за преглед.
        </p>
      </div>

      <div className="mt-4 flex flex-wrap items-end gap-3">
        <label className="flex w-full max-w-sm flex-col gap-1.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-tertiary">Търсене</span>
          <SearchInput
            containerClassName="w-full"
            value={state.searchTerm}
            onChange={(event) => actions.setSearchTerm(event.target.value)}
            placeholder="Търсене по съобщение, път или потребител"
          />
        </label>
        <label className="flex flex-col gap-1.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-tertiary">От</span>
          <input
            type="date"
            value={state.fromDate}
            max={state.toDate || undefined}
            onChange={(event) => actions.changeFromDate(event.target.value)}
            className="bank-input p-2.5 text-sm text-foreground"
          />
        </label>
        <label className="flex flex-col gap-1.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-tertiary">До</span>
          <input
            type="date"
            value={state.toDate}
            min={state.fromDate || undefined}
            onChange={(event) => actions.changeToDate(event.target.value)}
            className="bank-input p-2.5 text-sm text-foreground"
          />
        </label>
      </div>

      <ErrorsGrid errors={state.errors} isLoading={state.isLoading} onSelect={actions.openError} />

      {state.totalCount > 0 ? (
        <Pagination
          page={state.page}
          pageSize={state.pageSize}
          totalCount={state.totalCount}
          onPageChange={actions.goToPage}
        />
      ) : null}

      <ErrorDetailsModal error={state.selectedError} onClose={actions.closeError} />
    </PageBody>
  );
}
