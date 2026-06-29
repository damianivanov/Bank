import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { type LinkUserContext } from "@/pages/Customers";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { userManagementService } from "@/services/userManagementService";
import type { StaffUserGrid, StaffUserSummary } from "@/types";
import { createEditableUser } from "../utils/userAccess.utils";

const SEARCH_DEBOUNCE_MS = 250;
const PAGE_SIZE = 20;

// Faceted филтри: в рамките на facet -> OR, между facet-ите -> AND. Прилагат се сървърно.
export type ClientLinkFacet = "linked" | "unlinked";
export type StaffStatusFacet = "active" | "inactive";

const EMPTY_SUMMARY: StaffUserSummary = { total: 0, linked: 0, missingCustomer: 0, active: 0, inactive: 0 };

// Множествен facet -> nullable филтър: филтрираме само при точно един избран, иначе (0 или 2) не филтрираме.
function singleFacetToBool<T extends string>(filters: T[], trueKey: T): boolean | undefined {
  if (filters.length !== 1) {
    return undefined;
  }

  return filters[0] === trueKey;
}

export function useStaffUserManagementPage() {
  const [users, setUsers] = useState<StaffUserGrid[]>([]);
  const [summary, setSummary] = useState<StaffUserSummary>(EMPTY_SUMMARY);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [clientFilters, setClientFilters] = useState<ClientLinkFacet[]>([]);
  const [statusFilters, setStatusFilters] = useState<StaffStatusFacet[]>([]);
  const [reloadIndex, setReloadIndex] = useState(0);
  const [selectedUserId, setSelectedUserId] = useState<number | null>(null);
  // Контекст за свързване на клиент с потребител — отваря модала „Създай клиент“.
  const [linkUserContext, setLinkUserContext] = useState<LinkUserContext | null>(null);
  // Модал „Нов потребител“ (създаване на клиент на гише).
  const [isCreateUserOpen, setIsCreateUserOpen] = useState(false);

  const linkedParam = useMemo(() => singleFacetToBool(clientFilters, "linked"), [clientFilters]);
  const activeParam = useMemo(() => singleFacetToBool(statusFilters, "active"), [statusFilters]);

  // Дебоунс на търсенето: при писане изчакваме, прилагаме термина и връщаме на първа страница.
  useEffect(() => {
    const handle = window.setTimeout(() => {
      setAppliedSearch(searchTerm.trim());
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [searchTerm]);

  useEffect(() => {
    let isCancelled = false;
    setIsLoading(true);

    async function loadUsers() {
      try {
        const data = await userManagementService.getAllUsers(
          { page, pageSize: PAGE_SIZE, search: appliedSearch || undefined },
          linkedParam,
          activeParam,
        );
        if (!isCancelled) {
          setUsers(data.items);
          setTotalCount(data.totalCount);
          setSummary(data.summary);
        }
      } catch (error) {
        if (!isCancelled) {
          setUsers([]);
          setTotalCount(0);
          toast.error(getCommonModelErrorMessage(error, "Потребителите не можаха да бъдат заредени"));
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadUsers();

    return () => {
      isCancelled = true;
    };
  }, [page, appliedSearch, linkedParam, activeParam, reloadIndex]);

  const toggleClientFilter = useCallback((key: ClientLinkFacet) => {
    setClientFilters((current) =>
      current.includes(key) ? current.filter((entry) => entry !== key) : [...current, key],
    );
    setPage(1);
  }, []);
  const clearClientFilters = useCallback(() => {
    setClientFilters([]);
    setPage(1);
  }, []);

  const toggleStatusFilter = useCallback((key: StaffStatusFacet) => {
    setStatusFilters((current) =>
      current.includes(key) ? current.filter((entry) => entry !== key) : [...current, key],
    );
    setPage(1);
  }, []);
  const clearStatusFilters = useCallback(() => {
    setStatusFilters([]);
    setPage(1);
  }, []);

  // Детайлите се показват в popup от данните на реда (без допълнителна заявка).
  const openUserDetails = useCallback((userId: number) => setSelectedUserId(userId), []);
  const closeUserDetails = useCallback(() => setSelectedUserId(null), []);
  // Затваряме детайлите и отваряме модала за създаване на клиент, предварително попълнен с данните на потребителя.
  const createCustomer = useCallback(
    (userId: number) => {
      const user = users.find((entry) => entry.id === userId);
      if (!user) {
        return;
      }

      setSelectedUserId(null);
      setLinkUserContext({
        linkUserId: user.id,
        linkUserEmail: user.email,
        linkUserFirstName: user.firstName ?? undefined,
        linkUserLastName: user.lastName ?? undefined,
      });
    },
    [users],
  );
  const closeCreateCustomer = useCallback(() => setLinkUserContext(null), []);
  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const handleCustomerCreated = useCallback(() => {
    setLinkUserContext(null);
    // Презареждаме таблицата — потребителят вече е свързан с клиент.
    reload();
  }, [reload]);
  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  const openCreateUser = useCallback(() => setIsCreateUserOpen(true), []);
  const closeCreateUser = useCallback(() => setIsCreateUserOpen(false), []);
  const handleUserCreated = useCallback(() => {
    setIsCreateUserOpen(false);
    reload();
  }, [reload]);

  const selectedUser = useMemo(() => {
    const row = users.find((user) => user.id === selectedUserId);
    return row ? createEditableUser(row) : null;
  }, [users, selectedUserId]);

  const state = useMemo(
    () => ({
      isLoading,
      searchTerm,
      users,
      summary,
      totalCount,
      page,
      pageSize: PAGE_SIZE,
      clientFilters,
      statusFilters,
      selectedUser,
      linkUserContext,
      isCreateUserOpen,
    }),
    [isLoading, searchTerm, users, summary, totalCount, page, clientFilters, statusFilters, selectedUser, linkUserContext, isCreateUserOpen],
  );
  const actions = useMemo(
    () => ({
      setSearchTerm,
      openUserDetails,
      closeUserDetails,
      createCustomer,
      closeCreateCustomer,
      handleCustomerCreated,
      openCreateUser,
      closeCreateUser,
      handleUserCreated,
      reload,
      goToPage,
      toggleClientFilter,
      clearClientFilters,
      toggleStatusFilter,
      clearStatusFilters,
    }),
    [
      openUserDetails,
      closeUserDetails,
      createCustomer,
      closeCreateCustomer,
      handleCustomerCreated,
      openCreateUser,
      closeCreateUser,
      handleUserCreated,
      reload,
      goToPage,
      toggleClientFilter,
      clearClientFilters,
      toggleStatusFilter,
      clearStatusFilters,
    ],
  );

  return { state, actions };
}
