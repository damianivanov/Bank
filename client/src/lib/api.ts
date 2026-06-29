import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { clearRefreshSessionMarker, hasRefreshSessionMarker } from "@/lib/authSession";
import { unwrapCommonModel } from "@/lib/commonModel";
import type { AuthResponse, JsonData } from "@/types";

const apiBaseUrl = (import.meta.env.VITE_API_URL as string | undefined)?.trim() || "/api";

const api = axios.create({
  baseURL: apiBaseUrl,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: () => void;
  reject: (reason?: unknown) => void;
}> = [];

function processQueue(error?: unknown) {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
      return;
    }

    promise.resolve();
  });
  failedQueue = [];
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || !originalRequest || originalRequest._retry) {
      return Promise.reject(error);
    }

    const url = (originalRequest.url || "").toLowerCase();
    const isAuthEndpoint = url.includes("login") || url.includes("register") || url.includes("refresh");
    if (isAuthEndpoint) {
      return Promise.reject(error);
    }

    if (!hasRefreshSessionMarker()) {
      return Promise.reject(error);
    }

    if (isRefreshing) {
      await new Promise<void>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      });
      return api(originalRequest);
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const response = await api.post<JsonData<AuthResponse>>("auth/refresh");
      const authResponse = unwrapCommonModel(response, "Обновяването на сесията е неуспешно");
      // Новият токен носи актуалните роли; синхронизираме и кеширания потребител (меню/guards), за да
      // отразят промяна на достъпа без презареждане. Динамичен import избягва цикличен внос на store-а.
      const { useUserStore } = await import("@/stores/userStore");
      useUserStore.getState().setAuthenticatedUser(authResponse.user);
      processQueue();
      return api(originalRequest);
    } catch (refreshError) {
      clearRefreshSessionMarker();
      processQueue(refreshError);
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  },
);

export default api;
