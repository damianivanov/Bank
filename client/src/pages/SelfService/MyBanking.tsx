import { RefreshCw } from "lucide-react";
import { formatRepresentativeRole } from "@/lib/representativeRole";
import { CustomerTypeBadge, PageBody, VipBadge } from "@/shared/components";
import AccountHistoryModal from "./components/AccountHistoryModal";
import DepositRequestModal from "./components/DepositRequestModal";
import MyAccountsTable from "./components/MyAccountsTable";
import MyCreditsTable from "./components/MyCreditsTable";
import MyDepositRequestsPanel from "./components/MyDepositRequestsPanel";
import WithdrawModal from "./components/WithdrawModal";
import { useMyBankingPage } from "./hooks/useMyBankingPage";

export default function MyBanking() {
  const { state, actions } = useMyBankingPage();

  // Първоначално зареждане — още нямаме профил, който да покажем.
  if (state.isLoading && !state.profile) {
    return (
      <PageBody>
        <p className="text-sm text-secondary">Зареждане на вашия банков преглед...</p>
      </PageBody>
    );
  }

  if (!state.profile) {
    return (
      <PageBody>
        <div className="bank-panel rounded-2xl px-5 py-8 text-center">
          <p className="text-sm font-semibold text-rose-500">
            {state.error ?? "Не успяхме да заредим вашия банков преглед. Моля, опитайте отново по-късно."}
          </p>
          <button
            type="button"
            onClick={actions.reload}
            className="bank-secondary-btn mt-4 inline-flex items-center gap-2 bank-btn"
          >
            <RefreshCw className="h-4 w-4" />
            Опитай отново
          </button>
        </div>
      </PageBody>
    );
  }

  const profile = state.profile;
  const activeModal = state.activeModal;

  return (
    <PageBody>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{state.displayName}</h1>
        <p className="mt-1 text-sm text-secondary">
          {state.isCompany
            ? "Фирмен банков преглед - вашите сметки и кредити като юридическо лице."
            : "Вашият личен банков преглед - вашите сметки и кредити."}
        </p>

        {state.customers.length > 1 ? (
          <div className="mt-4 flex flex-wrap gap-2" role="tablist" aria-label="Изберете клиент">
            {state.customers.map((customer) => {
              const isActive = customer.id === state.selectedCustomerId;
              return (
                <button
                  key={customer.id}
                  type="button"
                  role="tab"
                  aria-selected={isActive}
                  disabled={state.isLoading}
                  onClick={() => actions.selectCustomer(customer.id)}
                  className={`bank-btn text-sm ${isActive ? "bank-primary-btn" : "bank-secondary-btn"}`}
                >
                  {customer.displayName}
                </button>
              );
            })}
          </div>
        ) : null}
      </div>

      <section className="bank-panel mt-6 rounded-2xl p-5">
        <div className="grid gap-4 md:grid-cols-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Тип сметка</p>
            <div className="mt-2">
              <CustomerTypeBadge customerType={profile.customerType} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">VIP</p>
            <div className="mt-2">
              <VipBadge isVip={profile.isVip} />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">
              {state.isCompany ? "ЕИК" : "ЕГН"}
            </p>
            <p className="mt-2 text-sm font-semibold">
              {state.isCompany ? profile.companyIdentifier : profile.personalIdentifier}
            </p>
          </div>
        </div>

        {state.isCompany && profile.representatives.length > 0 ? (
          <div className="mt-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Представители</p>
            <ul className="mt-2 space-y-1 text-sm text-secondary">
              {profile.representatives.map((representative) => (
                <li key={`${representative.personId}-${representative.role}`}>
                  <span className="font-semibold">
                    {[representative.firstName, representative.lastName].filter(Boolean).join(" ")}
                  </span>{" "}
                  ({representative.egn}) — {formatRepresentativeRole(representative.role)}
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </section>

      <section className="mt-6">
        <h2 className="mb-3 text-xl font-bold tracking-tight">Моите сметки</h2>
        <MyAccountsTable
          accounts={profile.accounts}
          onDeposit={actions.openDeposit}
          onWithdraw={actions.openWithdraw}
          onViewHistory={actions.openHistory}
        />
      </section>

      <section className="mt-6">
        <h2 className="mb-3 text-xl font-bold tracking-tight">Моите заявки за депозит</h2>
        <MyDepositRequestsPanel refreshSignal={state.refreshSignal} />
      </section>

      <section className="mt-6">
        <h2 className="mb-3 text-xl font-bold tracking-tight">Моите кредити</h2>
        <MyCreditsTable credits={profile.credits} />
      </section>

      <DepositRequestModal
        isOpen={activeModal?.type === "deposit"}
        account={activeModal?.type === "deposit" ? activeModal.account : null}
        onClose={actions.closeModal}
        onSubmitted={actions.reload}
      />
      <WithdrawModal
        isOpen={activeModal?.type === "withdraw"}
        account={activeModal?.type === "withdraw" ? activeModal.account : null}
        onClose={actions.closeModal}
        onCompleted={actions.reload}
      />
      <AccountHistoryModal
        isOpen={activeModal?.type === "history"}
        account={activeModal?.type === "history" ? activeModal.account : null}
        onClose={actions.closeModal}
      />
    </PageBody>
  );
}
