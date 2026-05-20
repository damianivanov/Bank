import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";

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

    if (isRefreshing) {
      return new Promise<void>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      }).then(() => api(originalRequest));
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const response = await api.post("auth/refresh");
      if (response.status === 200 && response.data?.success) {
        processQueue();
        return api(originalRequest);
      }

      throw new Error("Refresh failed");
    } catch (refreshError) {
      processQueue(refreshError);
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  },
);

export default api;
