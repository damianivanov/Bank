import { create } from "zustand";
import type { User } from "@/types";
import { authService } from "@/services/authService";
import { clearRefreshSessionMarker, hasRefreshSessionMarker, markRefreshSession } from "@/lib/authSession";

function createEmptyUser(): User {
  return {
    id: 0,
    email: "",
    mustChangePassword: false,
    roles: [],
  };
}

export interface UserState {
  user: User;
  userLoaded: boolean;
  isAuthenticated: boolean;
  initUser: () => Promise<void>;
  logout: () => Promise<void>;
  setAuthenticatedUser: (user: User) => void;
}

export const useUserStore = create<UserState>()((set) => ({
  user: createEmptyUser(),
  userLoaded: false,
  isAuthenticated: false,

  initUser: async () => {
    if (!hasRefreshSessionMarker()) {
      set({
        user: createEmptyUser(),
        userLoaded: true,
        isAuthenticated: false,
      });
      return;
    }

    let currentUser: User | null = null;

    try {
      const user = await authService.getCurrentUser();
      currentUser = user.id > 0 ? user : null;
    } catch {
      currentUser = null;
    }

    // current-user е [AllowAnonymous] и при изтекъл/обезсилен токен връща празен потребител със статус 200
    // (не 401), тъй че 401 интерсепторът не се задейства. Затова опитваме изричен refresh, преди да
    // изхвърлим сесията — иначе всяко отваряне след изтекъл access токен би било тих logout въпреки
    // валидния refresh токен. Чистим маркера само ако и самият refresh се провали.
    if (!currentUser) {
      try {
        const authResponse = await authService.refresh();
        currentUser = authResponse.user.id > 0 ? authResponse.user : null;
      } catch {
        currentUser = null;
      }
    }

    if (currentUser) {
      markRefreshSession();
    } else {
      clearRefreshSessionMarker();
    }

    set({
      user: currentUser ?? createEmptyUser(),
      userLoaded: true,
      isAuthenticated: Boolean(currentUser),
    });
  },

  logout: async () => {
    try {
      await authService.logout();
    } catch {}

    clearRefreshSessionMarker();

    set({
      user: createEmptyUser(),
      userLoaded: true,
      isAuthenticated: false,
    });
  },

  setAuthenticatedUser: (user) => {
    markRefreshSession();

    set({
      user,
      userLoaded: true,
      isAuthenticated: true,
    });
  },
}));
