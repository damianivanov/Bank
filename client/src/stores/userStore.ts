import { create } from "zustand";
import axios from "axios";
import type { User } from "@/types";
import { authService } from "@/services/authService";
import { clearRefreshSessionMarker, hasRefreshSessionMarker, markRefreshSession } from "@/lib/authSession";

function createEmptyUser(): User {
  return {
    id: 0,
    email: "",
    roles: [],
  };
}

function shouldClearRefreshSession(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 401;
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

    const loadCurrentUser = async (): Promise<User | null> => {
      const user = await authService.getCurrentUser();
      if (user.id <= 0) {
        return null;
      }

      return user;
    };

    try {
      currentUser = await loadCurrentUser();
    } catch {
      currentUser = null;
    }

    if (currentUser) {
      markRefreshSession();
    } else if (hasRefreshSessionMarker()) {
      try {
        await authService.refresh();
        markRefreshSession();
        currentUser = await loadCurrentUser();
      } catch (error) {
        if (shouldClearRefreshSession(error)) {
          clearRefreshSessionMarker();
        }

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
