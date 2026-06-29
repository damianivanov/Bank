import { useState } from "react";
import { ArrowLeft, Building2, Pencil, Plus, Star, User } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { formatRepresentativeRole } from "@/lib/representativeRole";
import { CopyableValue, CustomerTypeBadge, PageBody, VipBadge } from "@/shared/components";
import { CustomerType } from "@/types";
import AccountCreateModal from "@/pages/Accounts/components/AccountCreateModal";
import AccountDetailsModal from "@/pages/Accounts/components/AccountDetailsModal";
import CreditCreateModal from "@/pages/Credits/components/CreditCreateModal";
import CustomerAccountsTable from "./components/CustomerAccountsTable";
import CustomerCreditsTable from "./components/CustomerCreditsTable";
import CustomerUpsertModal from "./components/CustomerUpsertModal";
import { useCustomerDetailsPage } from "./hooks/useCustomerDetailsPage";

function initials(firstName: string, lastName: string): string {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase() || "?";
}

export default function CustomerDetails() {
  const { state, actions } = useCustomerDetailsPage();
  const navigate = useNavigate();
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isCreditModalOpen, setIsCreditModalOpen] = useState(false);
  const [openAccountId, setOpenAccountId] = useState<number | null>(null);

  if (state.isLoading || !state.customer) {
    return (
      <PageBody>
        <p className="text-sm text-secondary">Зареждане на клиент...</p>
      </PageBody>
    );
  }

  const customer = state.customer;
  const isCompany = customer.customerType === CustomerType.Company;
  const identifier = isCompany ? customer.companyIdentifier : customer.personalIdentifier;
  const identifierLabel = isCompany ? "ЕИК" : "ЕГН";

  return (
    <PageBody>
      <div className="flex items-center gap-3">
        <Link
          to="/customers"
          aria-label="Назад към клиентите"
          className="bank-secondary-btn inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg"
        >
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <h1 className="min-w-0 truncate text-3xl font-bold tracking-tight">{state.customerName}</h1>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-2">
        <button type="button" onClick={actions.openEditModal} className="bank-secondary-btn bank-btn">
          <Pencil className="h-4 w-4" />
          Редактирай
        </button>
        {state.canManageVip ? (
          <button
            type="button"
            onClick={actions.toggleVip}
            disabled={state.isUpdatingVip}
            className="bank-primary-btn bank-btn disabled:opacity-60"
          >
            <Star className="h-4 w-4" />
            {state.isUpdatingVip ? "Обновяване..." : customer.isVip ? "Премахни VIP" : "Маркирай като VIP"}
          </button>
        ) : null}
      </div>

      <section className="bank-panel mt-6 overflow-hidden rounded-2xl">
        {/* Идентичност: вид + VIP + идентификатор (с копиране) */}
        <div className="flex flex-wrap items-center justify-between gap-4 p-5 sm:p-6">
          <div className="flex min-w-0 items-center gap-3.5">
            <span className="bank-icon-tile-soft flex h-11 w-11 shrink-0 items-center justify-center rounded-xl">
              {isCompany ? <Building2 className="h-5 w-5" /> : <User className="h-5 w-5" />}
            </span>
            <div className="min-w-0">
              <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">{identifierLabel}</p>
              {identifier ? (
                <CopyableValue value={identifier} label={identifierLabel} className="text-base sm:text-lg" />
              ) : (
                <p className="text-base font-semibold sm:text-lg">-</p>
              )}
            </div>
          </div>
          <div className="flex items-center gap-2">
            <CustomerTypeBadge customerType={customer.customerType} />
            <VipBadge isVip={customer.isVip} />
          </div>
        </div>

        {isCompany && customer.representatives.length > 0 ? (
          <div className="border-t border-black/10 p-5 sm:p-6 dark:border-white/10">
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Представители</p>
            <ul className="mt-3 grid gap-2.5 sm:grid-cols-2">
              {customer.representatives.map((representative) => (
                <li
                  key={`${representative.personId}-${representative.role}`}
                  className="flex items-center gap-3 rounded-xl border border-black/10 bg-black/[0.02] p-3 dark:border-white/10 dark:bg-white/[0.03]"
                >
                  <span className="bank-icon-tile-soft flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-sm font-bold">
                    {initials(representative.firstName, representative.lastName)}
                  </span>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold">
                      {[representative.firstName, representative.lastName].filter(Boolean).join(" ")}
                    </p>
                    <p className="truncate text-xs text-tertiary">
                      <span className="font-mono tabular-nums">{representative.egn}</span> ·{" "}
                      {formatRepresentativeRole(representative.role)}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </section>

      <section className="mt-6">
        <div className="mb-3 flex items-center justify-between gap-3 md:justify-start">
          <h2 className="text-xl font-bold tracking-tight">Сметки</h2>
          <button
            type="button"
            onClick={() => setIsAccountModalOpen(true)}
            className="bank-secondary-btn bank-btn-action"
          >
            <Plus className="h-3.5 w-3.5" />
            Открий сметка
          </button>
        </div>
        <CustomerAccountsTable accounts={customer.accounts} onOpen={setOpenAccountId} />
      </section>

      <section className="mt-6">
        <div className="mb-3 flex items-center justify-between gap-3 md:justify-start">
          <h2 className="text-xl font-bold tracking-tight">Кредити</h2>
          <button
            type="button"
            onClick={() => setIsCreditModalOpen(true)}
            className="bank-secondary-btn bank-btn-action"
          >
            <Plus className="h-3.5 w-3.5" />
            Отпусни кредит
          </button>
        </div>
        <CustomerCreditsTable credits={customer.credits} />
      </section>

      <CustomerUpsertModal
        isOpen={state.isEditModalOpen}
        customerId={customer.id}
        onClose={actions.closeEditModal}
        onSaved={actions.handleCustomerSaved}
      />

      <AccountCreateModal
        isOpen={isAccountModalOpen}
        presetCustomerId={customer.id}
        presetCustomerDisplayName={state.customerName}
        onClose={() => setIsAccountModalOpen(false)}
        onCreated={(accountId) => {
          setIsAccountModalOpen(false);
          actions.handleCustomerSaved();
          setOpenAccountId(accountId);
        }}
      />

      <AccountDetailsModal
        isOpen={openAccountId !== null}
        accountId={openAccountId}
        onClose={() => setOpenAccountId(null)}
        onChanged={actions.handleCustomerSaved}
      />

      <CreditCreateModal
        isOpen={isCreditModalOpen}
        presetCustomerId={customer.id}
        presetCustomerIsVip={customer.isVip}
        onClose={() => setIsCreditModalOpen(false)}
        onCreated={(creditId) => {
          setIsCreditModalOpen(false);
          navigate(`/credits/${creditId}`);
        }}
      />
    </PageBody>
  );
}
