import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { isAdmin } from "@/lib/access";
import { type LinkUserContext } from "@/pages/Customers";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { userManagementService } from "@/services/userManagementService";
import { useUserStore } from "@/stores/userStore";
import { UserRole, type UserAccessSummary } from "@/types";
import {
  createEditableUser,
  getUserAccessPatch,
  type AccessOptionKey,
  type EditableUserAccess,
  type UserAccessPatch,
} from "../utils/userAccess.utils";

const SEARCH_DEBOUNCE_MS = 250;
const PAGE_SIZE = 20;

// Faceted филтри: в рамките на facet -> OR, между facet-ите -> AND. Прилагат се сървърно.
export type RoleFacet = "admin" | "staff" | "customer";
export type StatusFacet = "active" | "inactive";

const EMPTY_SUMMARY: UserAccessSummary = {
  totalUsers: 0,
  admins: 0,
  staff: 0,
  customers: 0,
  active: 0,
  inactive: 0,
};

const ROLE_FACET_TO_USER_ROLE: Record<RoleFacet, UserRole> = {
  admin: UserRole.Admin,
  staff: UserRole.Staff,
  customer: UserRole.Customer,
};

// Множествен статус facet -> nullable филтър: филтрираме само при точно един избран.
function singleFacetToBool<T extends string>(filters: T[], trueKey: T): boolean | undefined {
  if (filters.length !== 1) {
    return undefined;
  }

  return filters[0] === trueKey;
}

export function useAdminUserAccessManagementPage() {
  const currentUser = useUserStore((state) => state.user);
  const canManageAccess = isAdmin(currentUser);

  const [users, setUsers] = useState<EditableUserAccess[]>([]);
  const [summary, setSummary] = useState<UserAccessSummary>(EMPTY_SUMMARY);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [savingUserIds, setSavingUserIds] = useState<number[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [roleFilters, setRoleFilters] = useState<RoleFacet[]>([]);
  const [statusFilters, setStatusFilters] = useState<StatusFacet[]>([]);
  const [selectedUserId, setSelectedUserId] = useState<number | null>(null);
  // Контекст за свързване на клиент с потребител — отваря модала „Създай клиент“.
  const [linkUserContext, setLinkUserContext] = useState<LinkUserContext | null>(null);
  const [reloadIndex, setReloadIndex] = useState(0);
  // Монотонен брояч на заявките: и зареждащият ефект, и тихото опресняване след промяна на достъп
  // го увеличават при старт; данните се записват само от последната заявка (последната печели),
  // за да не презапише изостанал отговор по-нов и коректен резултат.
  const requestSeq = useRef(0);

  const rolesParam = useMemo(() => roleFilters.map((facet) => ROLE_FACET_TO_USER_ROLE[facet]), [roleFilters]);
  const activeParam = useMemo(() => singleFacetToBool(statusFilters, "active"), [statusFilters]);

  const setUserValue = useCallback((userId: number, patch: Partial<EditableUserAccess>) => {
    setUsers((currentUsers) => currentUsers.map((user) => (user.id === userId ? { ...user, ...patch } : user)));
  }, []);

  const markUserSaving = useCallback((userId: number) => {
    setSavingUserIds((currentUserIds) =>
      currentUserIds.includes(userId) ? currentUserIds : [...currentUserIds, userId],
    );
  }, []);

  const clearUserSaving = useCallback((userId: number) => {
    setSavingUserIds((currentUserIds) => currentUserIds.filter((entryId) => entryId !== userId));
  }, []);

  const isUserSaving = useCallback((userId: number) => savingUserIds.includes(userId), [savingUserIds]);

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
    const seq = ++requestSeq.current;
    setIsLoading(true);

    async function loadUsers() {
      try {
        const data = await userManagementService.getUsers(
          { page, pageSize: PAGE_SIZE, search: appliedSearch || undefined },
          rolesParam,
          activeParam,
        );
        if (seq === requestSeq.current) {
          setUsers(data.items.map(createEditableUser));
          setTotalCount(data.totalCount);
          setSummary(data.summary);
        }
      } catch (error) {
        if (seq === requestSeq.current) {
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
  }, [page, appliedSearch, rolesParam, activeParam, reloadIndex]);

  // След успешна промяна на достъп опресняваме страницата + обобщението тихо (без индикатор за
  // зареждане), за да отразим новите броячи и членството по филтрите, без да трепка таблицата.
  const refreshAfterAccessChange = useCallback(async () => {
    const seq = ++requestSeq.current;
    try {
      const data = await userManagementService.getUsers(
        { page, pageSize: PAGE_SIZE, search: appliedSearch || undefined },
        rolesParam,
        activeParam,
      );
      // Записваме само ако оттогава не е стартирала по-нова заявка (смяна на филтър/търсене/страница).
      if (seq === requestSeq.current) {
        setUsers(data.items.map(createEditableUser));
        setTotalCount(data.totalCount);
        setSummary(data.summary);
      }
    } catch {
      // При неуспех на опресняването запазваме оптимистичното състояние на реда.
    }
  }, [page, appliedSearch, rolesParam, activeParam]);

  const saveUserAccessChange = useCallback(
    async (userId: number, patch: Partial<UserAccessPatch>) => {
      const existingUser = users.find((entry) => entry.id === userId);
      if (!existingUser) {
        return;
      }

      const previousAccess = getUserAccessPatch(existingUser);
      const nextAccess: UserAccessPatch = { ...previousAccess, ...patch };

      setUserValue(userId, nextAccess);
      markUserSaving(userId);

      try {
        const updatedUser = await userManagementService.updateUserAccess(userId, nextAccess);
        setUserValue(userId, createEditableUser(updatedUser));
        await refreshAfterAccessChange();
      } catch (error) {
        setUserValue(userId, previousAccess);
        toast.error(getCommonModelErrorMessage(error, "Достъпът на потребителя не можа да бъде обновен"));
      } finally {
        clearUserSaving(userId);
      }
    },
    [clearUserSaving, markUserSaving, refreshAfterAccessChange, setUserValue, users],
  );

  const toggleAccessOption = useCallback(
    (userId: number, key: AccessOptionKey, nextValue: boolean) => {
      if (key === "isActive") {
        void saveUserAccessChange(userId, { isActive: nextValue });
        return;
      }

      if (key === "isStaff") {
        void saveUserAccessChange(userId, { isStaff: nextValue });
        return;
      }

      void saveUserAccessChange(userId, { isAdmin: nextValue });
    },
    [saveUserAccessChange],
  );

  const toggleUserActive = useCallback(
    (userId: number, isActive: boolean) => {
      void saveUserAccessChange(userId, { isActive: !isActive });
    },
    [saveUserAccessChange],
  );

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
  const handleCustomerCreated = useCallback(() => {
    setLinkUserContext(null);
    // Тихо опресняване — потребителят вече е свързан с лице, а броячите се променят.
    void refreshAfterAccessChange();
  }, [refreshAfterAccessChange]);
  const reload = useCallback(() => setReloadIndex((index) => index + 1), []);
  const goToPage = useCallback((nextPage: number) => setPage(nextPage), []);

  const toggleRoleFilter = useCallback((key: RoleFacet) => {
    setRoleFilters((current) =>
      current.includes(key) ? current.filter((entry) => entry !== key) : [...current, key],
    );
    setPage(1);
  }, []);
  const clearRoleFilters = useCallback(() => {
    setRoleFilters([]);
    setPage(1);
  }, []);

  const toggleStatusFilter = useCallback((key: StatusFacet) => {
    setStatusFilters((current) =>
      current.includes(key) ? current.filter((entry) => entry !== key) : [...current, key],
    );
    setPage(1);
  }, []);
  const clearStatusFilters = useCallback(() => {
    setStatusFilters([]);
    setPage(1);
  }, []);

  const selectedUser = useMemo(
    () => users.find((user) => user.id === selectedUserId) ?? null,
    [users, selectedUserId],
  );

  const state = useMemo(
    () => ({
      isLoading,
      canManageAccess,
      searchTerm,
      users,
      summary,
      totalCount,
      page,
      pageSize: PAGE_SIZE,
      roleFilters,
      statusFilters,
      selectedUser,
      linkUserContext,
    }),
    [isLoading, canManageAccess, searchTerm, users, summary, totalCount, page, roleFilters, statusFilters, selectedUser, linkUserContext],
  );
  const actions = useMemo(
    () => ({
      setSearchTerm,
      openUserDetails,
      closeUserDetails,
      createCustomer,
      closeCreateCustomer,
      handleCustomerCreated,
      reload,
      goToPage,
      isUserSaving,
      toggleAccessOption,
      toggleUserActive,
      toggleRoleFilter,
      clearRoleFilters,
      toggleStatusFilter,
      clearStatusFilters,
    }),
    [
      openUserDetails,
      closeUserDetails,
      createCustomer,
      closeCreateCustomer,
      handleCustomerCreated,
      reload,
      goToPage,
      isUserSaving,
      toggleAccessOption,
      toggleUserActive,
      toggleRoleFilter,
      clearRoleFilters,
      toggleStatusFilter,
      clearStatusFilters,
    ],
  );

  return { state, actions };
}
