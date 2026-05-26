import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from "axios";
import { useAuthStore } from "@/lib/stores/auth-store";

/**
 * Axios instance with interceptors:
 *  - Adds JWT Bearer token from Zustand auth store
 *  - Adds Accept-Language from current locale cookie/browser
 *  - On 401 → triggers refresh once, then retries (TODO: implement refresh flow in M01)
 */
function createApiClient(): AxiosInstance {
  const baseURL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

  const instance = axios.create({
    baseURL: `${baseURL}/api/v1`,
    timeout: 30_000,
    headers: { "Content-Type": "application/json" },
  });

  instance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = useAuthStore.getState().accessToken;
    if (token) {
      config.headers.set("Authorization", `Bearer ${token}`);
    }
    // Locale → Accept-Language header
    if (typeof document !== "undefined") {
      const locale = document.documentElement.lang || "vi";
      config.headers.set("Accept-Language", locale);
    }
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    (error) => {
      // TODO(M01): handle 401 → refresh token rotate → retry once.
      return Promise.reject(error);
    },
  );

  return instance;
}

export const apiClient = createApiClient();
