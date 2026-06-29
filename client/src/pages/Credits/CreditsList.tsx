import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Eye, Plus } from "lucide-react";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import {
  AsyncSection,
  CreditStatusBadge,
  EntityGrid,
  PageBody,
  PageHeader,
  Pagination,
  SearchInput,
  VipBadge,
} from "@/shared/components";
import { formatCreditType } from "./utils/creditDisplay";
import { useCreditsListPage } from "./hooks/useCreditsListPage";
import CreditCreateModal from "./components/CreditCreateModal";

export default function CreditsList() {
  const { state, actions } = useCreditsListPage();
  const navigate = useNavigate();
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  return (
    <PageBody>
      <PageHeader
        title="Кредити"
        subtitle="Отпускане и проследяване на потребителски и ипотечни кредити."
        actions={
          <button type="button" onClick={() => setIsCreateOpen(true)} className="bank-primary-btn bank-btn">
            <Plus className="h-4 w-4" />
            Отпусни кредит
          </button>
        }
      />

      <div className="mt-6 space-y-4">
        <SearchInput
          value={state.search}
          onChange={(event) => actions.changeSearch(event.target.value)}
          placeholder="Търсене по клиент"
        />

        <AsyncSection
          isLoading={state.isLoading}
          error={state.error}
          onRetry={actions.reload}
          isEmpty={state.credits.length === 0}
          loadingLabel="Зареждане на кредити..."
          emptyLabel={state.appliedSearch ? "Няма кредити за тази заявка." : "Няма кредити."}
        >
          <EntityGrid>
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
                <th className="px-4 py-3">Клиент</th>
                <th className="px-4 py-3">Вид</th>
                <th className="px-4 py-3">Сума</th>
                <th className="px-4 py-3">Лихвен процент</th>
                <th className="px-4 py-3">VIP при създаване</th>
                <th className="px-4 py-3">Статус</th>
                <th className="px-4 py-3">Отпуснат на</th>
                <th className="px-4 py-3 text-right">Действие</th>
              </tr>
            </thead>
            <tbody>
              {state.credits.map((credit) => (
                <tr key={credit.id} className="border-b border-slate-100 text-sm last:border-b-0">
                  <td className="px-4 py-3 font-semibold">{credit.customerDisplayName}</td>
                  <td className="px-4 py-3">{formatCreditType(credit.creditType)}</td>
                  <td className="px-4 py-3">{formatCurrency(credit.grantedAmount)}</td>
                  <td className="px-4 py-3">{formatPercent(credit.appliedAnnualInterestRate)}</td>
                  <td className="px-4 py-3">
                    <VipBadge isVip={credit.customerWasVipAtCreation} />
                  </td>
                  <td className="px-4 py-3">
                    <CreditStatusBadge status={credit.status} />
                  </td>
                  <td className="px-4 py-3">{formatDate(credit.grantedAtUtc)}</td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      to={`/credits/${credit.id}`}
                      className="bank-secondary-btn bank-btn-action"
                    >
                      <Eye className="h-3.5 w-3.5" />
                      Отвори
                    </Link>
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

      <CreditCreateModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={(creditId) => {
          setIsCreateOpen(false);
          navigate(`/credits/${creditId}`);
        }}
      />
    </PageBody>
  );
}
