import { useCallback, useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { myBankingService } from "@/services/myBankingService";
import { CustomerType, type CustomerAccountSummary, type CustomerDetails, type CustomerLookup } from "@/types";

type AccountModalType = "deposit" | "withdraw" | "history";

type ActiveAccountModal = {
  type: AccountModalType;
  account: CustomerAccountSummary;
};

export function useMyBankingPage() {
  const [customers, setCustomers] = useState<CustomerLookup[]>([]);
  const [selectedCustomerId, setSelectedCustomerId] = useState<number | null>(null);
  const [profile, setProfile] = useState<CustomerDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadIndex, setReloadIndex] = useState(0);
  const [activeModal, setActiveModal] = useState<ActiveAccountModal | null>(null);

  // Списъкът с достъпни клиенти (собствено физическо лице + представлявани фирми) за превключвателя.
  useEffect(() => {
    let isCancelled = false;

    async function loadCustomers() {
      try {
        const list = await myBankingService.getAccessibleCustomers();
        if (isCancelled) {
          return;
        }

        setCustomers(list);
        // Запазваме текущия избор, ако още е валиден; иначе по подразбиране — първият (собственото лице).
        setSelectedCustomerId((current) =>
          current != null && list.some((customer) => customer.id === current) ? current : list[0]?.id ?? null,
        );

        if (list.length === 0) {
          setProfile(null);
          setError("Към този акаунт няма свързан клиент.");
          setIsLoading(false);
        }
      } catch (loadError) {
        if (isCancelled) {
          return;
        }

        setCustomers([]);
        setSelectedCustomerId(null);
        setProfile(null);
        setError(getCommonModelErrorMessage(loadError, "Вашият банков преглед не можа да бъде зареден"));
        setIsLoading(false);
      }
    }

    void loadCustomers();

    return () => {
      isCancelled = true;
    };
  }, [reloadIndex]);

  // Профилът на избрания клиент. Презарежда се при смяна на избора или при ръчно презареждане.
  useEffect(() => {
    if (selectedCustomerId == null) {
      return;
    }

    let isCancelled = false;

    async function loadProfile(customerId: number) {
      setIsLoading(true);
      setError(null);

      try {
        const profileData = await myBankingService.getProfile(customerId);
        if (!isCancelled) {
          setProfile(profileData);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setProfile(null);
          setError(getCommonModelErrorMessage(loadError, "Вашият банков преглед не можа да бъде зареден"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadProfile(selectedCustomerId);

    return () => {
      isCancelled = true;
    };
  }, [selectedCustomerId, reloadIndex]);

  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const selectCustomer = useCallback((customerId: number) => setSelectedCustomerId(customerId), []);

  const openDeposit = useCallback((account: CustomerAccountSummary) => setActiveModal({ type: "deposit", account }), []);
  const openWithdraw = useCallback((account: CustomerAccountSummary) => setActiveModal({ type: "withdraw", account }), []);
  const openHistory = useCallback((account: CustomerAccountSummary) => setActiveModal({ type: "history", account }), []);
  const closeModal = useCallback(() => setActiveModal(null), []);

  const isCompany = profile?.customerType === CustomerType.Company;
  const displayName = profile
    ? isCompany
      ? profile.companyName ?? ""
      : `${profile.firstName ?? ""} ${profile.lastName ?? ""}`.trim()
    : "";

  const state = useMemo(
    () => ({
      profile,
      customers,
      selectedCustomerId,
      isLoading,
      error,
      isCompany,
      displayName,
      activeModal,
      refreshSignal: reloadIndex,
    }),
    [profile, customers, selectedCustomerId, isLoading, error, isCompany, displayName, activeModal, reloadIndex],
  );
  const actions = useMemo(
    () => ({ reload, selectCustomer, openDeposit, openWithdraw, openHistory, closeModal }),
    [reload, selectCustomer, openDeposit, openWithdraw, openHistory, closeModal],
  );

  return { state, actions };
}
