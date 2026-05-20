import { create } from "zustand";
import type { User } from "@/types";
import { authService } from "@/services/authService";

function createEmptyUser(): User {
  return {
    id: 0,
    email: "",
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
    let currentUser: User | null = null;

    const tryGetCurrentUser = async (): Promise<User | null> => {
      const response = await authService.getCurrentUser();
      const result = response.data;

      if (!result.success || !result.data || result.data.id <= 0) {
        return null;
      }

      return result.data;
    };

    try {
      currentUser = await tryGetCurrentUser();
    } catch {
      currentUser = null;
    }

    if (!currentUser) {
      try {
        const refreshResponse = await authService.refresh();
        if (refreshResponse.data.success) {
          currentUser = await tryGetCurrentUser();
        }
      } catch {
        currentUser = null;
      }
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
    } catch {
      // Keep client state deterministic even when the server request fails.
    }

    set({
      user: createEmptyUser(),
      userLoaded: true,
      isAuthenticated: false,
    });
  },

  setAuthenticatedUser: (user) => {
    set({
      user,
      userLoaded: true,
      isAuthenticated: true,
    });
  },
}));
