import { useCallback, useEffect, useMemo, useState } from "react";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { customerService } from "@/services/customerService";
import { CustomerType, type Customer } from "@/types";

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

export function useCustomersListPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [customerType, setCustomerType] = useState<CustomerType | undefined>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadIndex, setReloadIndex] = useState(0);
  const [isCustomerModalOpen, setIsCustomerModalOpen] = useState(false);
  const [editingCustomerId, setEditingCustomerId] = useState<number | null>(null);

  // При писане в полето изчакваме кратко, нулираме страницата и едва тогава прилагаме търсенето,
  // за да не пращаме заявка при всеки натиснат клавиш.
  useEffect(() => {
    const handle = window.setTimeout(() => {
      setAppliedSearch(searchInput.trim());
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    let isCancelled = false;

    async function loadCustomers() {
      setIsLoading(true);
      setError(null);

      try {
        const data = await customerService.getCustomers(
          { page, pageSize: PAGE_SIZE, search: appliedSearch || undefined },
          customerType,
        );
        if (!isCancelled) {
          setCustomers(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setCustomers([]);
          setTotalCount(0);
          setError(getCommonModelErrorMessage(loadError, "Клиентите не могат да бъдат заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadCustomers();

    return () => {
      isCancelled = true;
    };
  }, [page, appliedSearch, customerType, reloadIndex]);

  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const changeSearch = useCallback((value: string) => setSearchInput(value), []);
  const changeCustomerType = useCallback((value: CustomerType | undefined) => {
    setCustomerType(value);
    setPage(1);
  }, []);
  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  const openNewCustomerModal = useCallback(() => {
    setEditingCustomerId(null);
    setIsCustomerModalOpen(true);
  }, []);

  const openEditCustomerModal = useCallback((customerId: number) => {
    setEditingCustomerId(customerId);
    setIsCustomerModalOpen(true);
  }, []);

  const closeCustomerModal = useCallback(() => {
    setIsCustomerModalOpen(false);
    setEditingCustomerId(null);
  }, []);

  const handleCustomerSaved = useCallback(() => {
    reload();
  }, [reload]);

  const state = useMemo(
    () => ({
      customers,
      totalCount,
      page,
      pageSize: PAGE_SIZE,
      search: searchInput,
      appliedSearch,
      customerType,
      isLoading,
      error,
      isCustomerModalOpen,
      editingCustomerId,
    }),
    [
      customers,
      totalCount,
      page,
      searchInput,
      appliedSearch,
      customerType,
      isLoading,
      error,
      isCustomerModalOpen,
      editingCustomerId,
    ],
  );

  const actions = useMemo(
    () => ({
      reload,
      changeSearch,
      changeCustomerType,
      goToPage,
      openNewCustomerModal,
      openEditCustomerModal,
      closeCustomerModal,
      handleCustomerSaved,
    }),
    [
      reload,
      changeSearch,
      changeCustomerType,
      goToPage,
      openNewCustomerModal,
      openEditCustomerModal,
      closeCustomerModal,
      handleCustomerSaved,
    ],
  );

  return { state, actions };
}
