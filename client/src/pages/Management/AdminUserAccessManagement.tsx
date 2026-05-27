import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent } from "react";
import { LuCheck, LuChevronDown } from "react-icons/lu";
import { createPortal } from "react-dom";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { isAdmin } from "@/lib/access";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import {
  accessOptionLabels,
  createEditableUser,
  formatRole,
  formatUserName,
  getUserAccessPatch,
  type AccessOptionKey,
  type EditableUserAccess,
  type UserAccessPatch,
} from "./userAccess.utils";
import UserRoleBadges from "./UserRoleBadges";
import { userManagementService } from "@/services/userManagementService";
import { useUserStore } from "@/stores/userStore";
import { EntityGrid } from "@/shared/components";

const SEARCH_DEBOUNCE_MS = 250;
const ACCESS_MENU_VIEWPORT_PADDING_PX = 12;
const ACCESS_MENU_OFFSET_PX = 8;
const ACCESS_MENU_MIN_HEIGHT_PX = 120;
const ACCESS_MENU_MAX_HEIGHT_PX = 280;

type AccessDropdownProps = {
  disabled: boolean;
  values: UserAccessPatch;
  onToggleOption: (key: AccessOptionKey, nextValue: boolean) => void;
};

type AccessMenuPosition = {
  top: number;
  left: number;
  width: number;
  maxHeight: number;
  placement: "top" | "bottom";
};

function joinClassNames(...classNames: Array<string | false | null | undefined>) {
  return classNames.filter(Boolean).join(" ");
}

function AccessDropdown({ disabled, values, onToggleOption }: AccessDropdownProps) {
  const rootRef = useRef<HTMLDivElement | null>(null);
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState<AccessMenuPosition | null>(null);

  useEffect(() => {
    if (disabled && isOpen) {
      setIsOpen(false);
    }
  }, [disabled, isOpen]);

  const updateMenuPosition = useCallback(() => {
    if (!triggerRef.current) {
      return;
    }

    const triggerRect = triggerRef.current.getBoundingClientRect();
    const availableBelow = window.innerHeight - triggerRect.bottom - ACCESS_MENU_VIEWPORT_PADDING_PX - ACCESS_MENU_OFFSET_PX;
    const availableAbove = triggerRect.top - ACCESS_MENU_VIEWPORT_PADDING_PX - ACCESS_MENU_OFFSET_PX;
    const shouldOpenAbove = availableBelow < 180 && availableAbove > availableBelow;
    const availableSpace = shouldOpenAbove ? availableAbove : availableBelow;
    const maxHeight = Math.max(
      ACCESS_MENU_MIN_HEIGHT_PX,
      Math.min(ACCESS_MENU_MAX_HEIGHT_PX, availableSpace),
    );

    const top = shouldOpenAbove
      ? triggerRect.top - ACCESS_MENU_OFFSET_PX
      : triggerRect.bottom + ACCESS_MENU_OFFSET_PX;

    setMenuPosition({
      top,
      left: triggerRect.left,
      width: triggerRect.width,
      maxHeight,
      placement: shouldOpenAbove ? "top" : "bottom",
    });
  }, []);

  useEffect(() => {
    if (!isOpen) {
      setMenuPosition(null);
      return;
    }

    updateMenuPosition();

    const handleViewportChange = () => {
      updateMenuPosition();
    };

    window.addEventListener("resize", handleViewportChange);
    window.addEventListener("scroll", handleViewportChange, true);

    return () => {
      window.removeEventListener("resize", handleViewportChange);
      window.removeEventListener("scroll", handleViewportChange, true);
    };
  }, [isOpen, updateMenuPosition]);

  useEffect(() => {
    const handleDocumentMouseDown = (event: MouseEvent) => {
      const isClickInsideRoot = rootRef.current?.contains(event.target as Node) ?? false;
      const isClickInsideMenu = menuRef.current?.contains(event.target as Node) ?? false;

      if (!isClickInsideRoot && !isClickInsideMenu) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleDocumentMouseDown);
    return () => {
      document.removeEventListener("mousedown", handleDocumentMouseDown);
    };
  }, []);

  const handleTriggerClick = () => {
    if (disabled) {
      return;
    }

    setIsOpen((currentValue) => !currentValue);
  };

  const handleActiveOptionClick = () => {
    onToggleOption("isActive", !values.isActive);
    setIsOpen(false);
  };

  const handleStaffOptionClick = () => {
    onToggleOption("isStaff", !values.isStaff);
    setIsOpen(false);
  };

  const handleAdminOptionClick = () => {
    onToggleOption("isAdmin", !values.isAdmin);
    setIsOpen(false);
  };

  const selectedLabels = (Object.keys(accessOptionLabels) as AccessOptionKey[])
    .filter((key) => values[key])
    .map((key) => accessOptionLabels[key]);

  const triggerLabel = selectedLabels.length > 0 ? selectedLabels.join(", ") : "None";

  return (
    <div ref={rootRef} className="relative">
      <button
        ref={triggerRef}
        type="button"
        disabled={disabled}
        onClick={handleTriggerClick}
        className={joinClassNames(
          "bank-select-trigger flex w-full items-center justify-between gap-2 rounded-full! px-3 py-2 text-xs font-semibold",
          disabled && "bank-select-trigger-disabled",
        )}
      >
        <span className="truncate">{triggerLabel}</span>
        <LuChevronDown className={joinClassNames("bank-select-chevron h-4 w-4", isOpen && "rotate-180")} />
      </button>

      {isOpen && menuPosition
        ? createPortal(
            <div
              ref={menuRef}
              className={joinClassNames(
                "bank-select-menu bank-access-select-menu fixed z-[70] overflow-hidden rounded-xl p-1",
                menuPosition.placement === "top" ? "bank-select-menu-enter-up origin-bottom" : "bank-select-menu-enter-down origin-top",
              )}
              style={{
                top: menuPosition.top,
                left: menuPosition.left,
                width: menuPosition.width,
                transform: menuPosition.placement === "top" ? "translateY(-100%)" : undefined,
              }}
            >
              <ul className="bank-select-list" style={{ maxHeight: menuPosition.maxHeight }}>
                <li className="bank-select-option-item">
                  <button
                    type="button"
                    onClick={handleActiveOptionClick}
                    className={joinClassNames("bank-select-option", values.isActive && "bank-select-option-selected")}
                  >
                    <span className="bank-select-option-label">{accessOptionLabels.isActive}</span>
                    {values.isActive ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                  </button>
                </li>
                <li className="bank-select-option-item">
                  <button
                    type="button"
                    onClick={handleStaffOptionClick}
                    className={joinClassNames("bank-select-option", values.isStaff && "bank-select-option-selected")}
                  >
                    <span className="bank-select-option-label">{accessOptionLabels.isStaff}</span>
                    {values.isStaff ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                  </button>
                </li>
                <li className="bank-select-option-item">
                  <button
                    type="button"
                    onClick={handleAdminOptionClick}
                    className={joinClassNames("bank-select-option", values.isAdmin && "bank-select-option-selected")}
                  >
                    <span className="bank-select-option-label">{accessOptionLabels.isAdmin}</span>
                    {values.isAdmin ? <LuCheck className="bank-select-check h-4 w-4 shrink-0" /> : null}
                  </button>
                </li>
              </ul>
            </div>,
            document.body,
          )
        : null}
    </div>
  );
}

function renderUserStatusBadge(isActive: boolean) {
  return (
    <span
      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
        isActive ? "bank-chip bank-chip-success" : "bank-chip bank-chip-warn"
      }`}
    >
      {isActive ? "Active" : "Inactive"}
    </span>
  );
}

function getActiveActionLabel(isActive: boolean, isRowLoading: boolean) {
  if (isRowLoading) {
    return "Saving...";
  }

  return isActive ? "Deactivate" : "Activate";
}

function getActiveActionButtonClassName(isActive: boolean) {
  const variantClassName = isActive ? "bank-danger-btn" : "bank-primary-btn";
  return `${variantClassName} inline-flex items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold disabled:cursor-not-allowed disabled:opacity-60`;
}

export default function AdminUserAccessManagementPage() {
  const navigate = useNavigate();
  const currentUser = useUserStore((state) => state.user);
  const canManageAccess = isAdmin(currentUser);

  const [users, setUsers] = useState<EditableUserAccess[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [savingUserIds, setSavingUserIds] = useState<number[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState("");

  const setUserValue = (userId: number, patch: Partial<EditableUserAccess>) => {
    setUsers((currentUsers) =>
      currentUsers.map((user) => (user.id === userId ? { ...user, ...patch } : user)),
    );
  };

  const markUserSaving = (userId: number) => {
    setSavingUserIds((currentUserIds) => {
      if (currentUserIds.includes(userId)) {
        return currentUserIds;
      }

      return [...currentUserIds, userId];
    });
  };

  const clearUserSaving = (userId: number) => {
    setSavingUserIds((currentUserIds) => currentUserIds.filter((entryId) => entryId !== userId));
  };

  const isUserSaving = (userId: number) => savingUserIds.includes(userId);

  const handleSearchTermChange = (event: ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
  };

  const createOpenUserDetailsHandler = (userId: number) => () => {
    navigate(`/users/${userId}`);
  };

  const loadUsers = async () => {
    setIsLoading(true);

    try {
      const usersData = await userManagementService.getUsers();
      setUsers(usersData.map(createEditableUser));
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load users"));
    } finally {
      setIsLoading(false);
    }
  };

  const saveUserAccessChange = async (userId: number, patch: Partial<UserAccessPatch>) => {
    const existingUser = users.find((entry) => entry.id === userId);
    if (!existingUser) {
      return;
    }

    const previousAccess = getUserAccessPatch(existingUser);
    const nextAccess: UserAccessPatch = {
      ...previousAccess,
      ...patch,
    };

    setUserValue(userId, nextAccess);
    markUserSaving(userId);

    try {
      const updatedUser = await userManagementService.updateUserAccess(userId, nextAccess);
      setUserValue(userId, createEditableUser(updatedUser));
    } catch (error) {
      setUserValue(userId, previousAccess);
      toast.error(getCommonModelErrorMessage(error, "Could not update user access"));
    } finally {
      clearUserSaving(userId);
    }
  };

  const createAccessOptionToggleHandler =
    (userId: number) =>
    (key: AccessOptionKey, nextValue: boolean) => {
      if (key === "isActive") {
        void saveUserAccessChange(userId, { isActive: nextValue });
        return;
      }

      if (key === "isStaff") {
        void saveUserAccessChange(userId, { isStaff: nextValue });
        return;
      }

      void saveUserAccessChange(userId, { isAdmin: nextValue });
    };

  const createToggleUserActiveHandler = (userId: number, isActive: boolean) => () => {
    void saveUserAccessChange(userId, { isActive: !isActive });
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchTerm]);

  const filteredUsers = useMemo(() => {
    const normalizedSearchTerm = debouncedSearchTerm.trim().toLowerCase();

    return users.filter((user) => {
      if (!normalizedSearchTerm) {
        return true;
      }

      const searchHaystack = [
        user.email,
        user.firstName,
        user.lastName,
        user.customerDisplayName,
        ...user.roles.map((role) => formatRole(role)),
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return searchHaystack.includes(normalizedSearchTerm);
    });
  }, [debouncedSearchTerm, users]);

  const summary = useMemo(() => {
    const inactive = users.filter((user) => !user.isActive).length;
    const admins = users.filter((user) => user.isAdmin).length;
    const staff = users.filter((user) => user.isStaff).length;

    return {
      totalUsers: users.length,
      admins,
      staff,
      inactive,
    };
  }, [users]);

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Admin User Management</h1>
        <p className="mt-1 text-sm text-secondary">
          Grant admin/staff access, invalidate accounts, and open user profiles.
        </p>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-2">
        <span className="bank-chip rounded-full px-3 py-1 text-xs font-semibold">Users: {summary.totalUsers}</span>
        <span className="bank-chip bank-chip-danger rounded-full px-3 py-1 text-xs font-semibold">Admins: {summary.admins}</span>
        <span className="bank-chip bank-chip-info rounded-full px-3 py-1 text-xs font-semibold">Staff: {summary.staff}</span>
        <span className="bank-chip bank-chip-warn rounded-full px-3 py-1 text-xs font-semibold">Inactive: {summary.inactive}</span>
      </div>

      <div className="mt-4">
        <input
          type="search"
          value={searchTerm}
          onChange={handleSearchTermChange}
          placeholder="Search by email, name, or role..."
          className="bank-input w-full rounded-full! px-3 py-2.5 text-sm lg:max-w-md"
        />
      </div>

      <div className="mt-6 md:hidden">
        {isLoading ? (
          <p className="text-sm text-secondary">Loading users...</p>
        ) : filteredUsers.length === 0 ? (
          <p className="text-sm text-secondary">No users match your search.</p>
        ) : (
          <div className="space-y-3">
            {filteredUsers.map((user) => {
              const isRowLoading = isUserSaving(user.id);
              const isAccessReadOnly = isRowLoading || !canManageAccess;
              const openUserDetailsHandler = createOpenUserDetailsHandler(user.id);
              const toggleUserActiveHandler = createToggleUserActiveHandler(user.id, user.isActive);
              const toggleActiveLabel = getActiveActionLabel(user.isActive, isRowLoading);
              const toggleActiveClassName = getActiveActionButtonClassName(user.isActive);

              return (
                <article key={user.id} className="bank-panel rounded-2xl p-4">
                  <div className="flex items-start justify-between gap-3">
                    <button
                      type="button"
                      onClick={openUserDetailsHandler}
                      className="min-w-0 cursor-pointer text-left text-sm font-semibold text-foreground underline-offset-4 transition hover:underline"
                    >
                      {user.email}
                    </button>
                    <button
                      type="button"
                      onClick={openUserDetailsHandler}
                      className="bank-secondary-btn inline-flex shrink-0 items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold"
                    >
                      View details
                    </button>
                  </div>

                  <p className="mt-2 text-sm text-secondary">{formatUserName(user)}</p>

                  <div className="mt-3">
                    <UserRoleBadges user={user} />
                  </div>

                  <div className="mt-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Status</p>
                    <div className="mt-1">{renderUserStatusBadge(user.isActive)}</div>
                  </div>

                  <div className="mt-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Access</p>
                    <div className="mt-1">
                      <AccessDropdown
                        disabled={isAccessReadOnly}
                        values={{
                          isActive: user.isActive,
                          isStaff: user.isStaff,
                          isAdmin: user.isAdmin,
                        }}
                        onToggleOption={createAccessOptionToggleHandler(user.id)}
                      />
                    </div>
                  </div>

                  <div className="mt-3">
                    <button
                      type="button"
                      disabled={isAccessReadOnly}
                      onClick={toggleUserActiveHandler}
                      className={toggleActiveClassName}
                    >
                      {toggleActiveLabel}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </div>

      <div className="mt-6 hidden md:block">
        {isLoading ? (
          <p className="text-sm text-secondary">Loading users...</p>
        ) : (
          <EntityGrid>
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
                <th className="px-4 py-3 text-left">Actions</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Roles</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Access</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => {
                const isRowLoading = isUserSaving(user.id);
                const isAccessReadOnly = isRowLoading || !canManageAccess;
                const openUserDetailsHandler = createOpenUserDetailsHandler(user.id);
                const toggleUserActiveHandler = createToggleUserActiveHandler(user.id, user.isActive);
                const toggleActiveLabel = getActiveActionLabel(user.isActive, isRowLoading);
                const toggleActiveClassName = getActiveActionButtonClassName(user.isActive);

                return (
                  <tr key={user.id} className="border-b border-slate-100 text-sm last:border-b-0">
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap justify-start gap-2">
                        <button
                          type="button"
                          onClick={openUserDetailsHandler}
                          className="bank-secondary-btn inline-flex items-center justify-center rounded-full px-3 py-2.5 text-xs font-semibold"
                        >
                          View details
                        </button>
                        <button
                          type="button"
                          disabled={isAccessReadOnly}
                          onClick={toggleUserActiveHandler}
                          className={toggleActiveClassName}
                        >
                          {toggleActiveLabel}
                        </button>
                      </div>
                    </td>
                    <td className="px-4 py-3 font-medium">
                      <button
                        type="button"
                        onClick={openUserDetailsHandler}
                        className="cursor-pointer text-left text-foreground underline-offset-4 transition hover:underline"
                      >
                        {user.email}
                      </button>
                    </td>
                    <td className="px-4 py-3">{formatUserName(user)}</td>
                    <td className="px-4 py-3">
                      <UserRoleBadges user={user} />
                    </td>
                    <td className="px-4 py-3">{renderUserStatusBadge(user.isActive)}</td>
                    <td className="px-4 py-3">
                      <div className="w-56">
                        <AccessDropdown
                          disabled={isAccessReadOnly}
                          values={{
                            isActive: user.isActive,
                            isStaff: user.isStaff,
                            isAdmin: user.isAdmin,
                          }}
                          onToggleOption={createAccessOptionToggleHandler(user.id)}
                        />
                      </div>
                    </td>
                  </tr>
                );
              })}
              {filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-secondary">
                    No users match your search.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </EntityGrid>
        )}
      </div>
    </section>
  );
}
