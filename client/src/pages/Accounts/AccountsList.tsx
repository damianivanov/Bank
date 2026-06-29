import { useState } from "react";
import { Eye, Plus } from "lucide-react";
import { formatCurrency, formatDate } from "@/lib/formatters";
import {
  AccountStatusBadge,
  AsyncSection,
  EntityGrid,
  PageBody,
  PageHeader,
  Pagination,
  SearchInput,
} from "@/shared/components";
import { useAccountsListPage } from "./hooks/useAccountsListPage";
import AccountCreateModal from "./components/AccountCreateModal";
import AccountDetailsModal from "./components/AccountDetailsModal";

export default function AccountsList() {
  const { state, actions } = useAccountsListPage();
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [openAccountId, setOpenAccountId] = useState<number | null>(null);

  return (
    <PageBody>
      <PageHeader
        title="Сметки"
        subtitle="Откривайте и следете банковите сметки на клиентите."
        actions={
          <button type="button" onClick={() => setIsCreateOpen(true)} className="bank-primary-btn bank-btn">
            <Plus className="h-4 w-4" />
            Открий сметка
          </button>
        }
      />

      <div className="mt-6 space-y-4">
        <SearchInput
          value={state.search}
          onChange={(event) => actions.changeSearch(event.target.value)}
          placeholder="Търсене по IBAN или клиент"
        />

        <AsyncSection
          isLoading={state.isLoading}
          error={state.error}
          onRetry={actions.reload}
          isEmpty={state.accounts.length === 0}
          loadingLabel="Зареждане на сметки..."
          emptyLabel={state.appliedSearch ? "Няма сметки за тази заявка." : "Няма сметки все още."}
        >
          <EntityGrid>
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
                <th className="px-4 py-3">IBAN</th>
                <th className="px-4 py-3">Клиент</th>
                <th className="px-4 py-3">Салдо</th>
                <th className="px-4 py-3">Статус</th>
                <th className="px-4 py-3">Открита на</th>
                <th className="px-4 py-3 text-right">Действие</th>
              </tr>
            </thead>
            <tbody>
              {state.accounts.map((account) => (
                <tr key={account.id} className="border-b border-slate-100 text-sm last:border-b-0">
                  <td className="px-4 py-3 font-mono text-xs">{account.iban}</td>
                  <td className="px-4 py-3 font-semibold">{account.customerDisplayName}</td>
                  <td className="px-4 py-3">{formatCurrency(account.balance)}</td>
                  <td className="px-4 py-3">
                    <AccountStatusBadge status={account.status} />
                  </td>
                  <td className="px-4 py-3">{formatDate(account.openedAtUtc)}</td>
                  <td className="px-4 py-3 text-right">
                    <button
                      type="button"
                      onClick={() => setOpenAccountId(account.id)}
                      className="bank-secondary-btn bank-btn-action"
                    >
                      <Eye className="h-3.5 w-3.5" />
                      Отвори
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </EntityGrid>
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

      <AccountCreateModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={(accountId) => {
          setIsCreateOpen(false);
          actions.reload();
          setOpenAccountId(accountId);
        }}
      />

      <AccountDetailsModal
        isOpen={openAccountId !== null}
        accountId={openAccountId}
        onClose={() => setOpenAccountId(null)}
        onChanged={actions.reload}
      />
    </PageBody>
  );
}
