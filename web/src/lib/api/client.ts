import axios, {
  type AxiosError,
  type AxiosInstance,
  type InternalAxiosRequestConfig,
} from "axios";
import { useAuthStore } from "@/lib/stores/auth-store";

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

/**
 * Single-flight refresh: concurrent 401s share ONE /auth/refresh call so we don't
 * trigger reuse-detection (which would revoke every session). Resolves to the new
 * access token, or null if refresh is impossible (no token / refresh rejected).
 */
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(apiBase: string): Promise<string | null> {
  const { refreshToken, setTokens, clear } = useAuthStore.getState();
  if (!refreshToken) {
    clear();
    return null;
  }

  try {
    // Bare axios (no interceptors) to avoid a 401 → refresh → 401 loop.
    const resp = await axios.post(`${apiBase}/api/v1/auth/refresh`, { refreshToken });
    const data = resp.data?.data as
      | { accessToken: string; refreshToken: string }
      | undefined;

    if (!data?.accessToken) {
      clear();
      return null;
    }

    setTokens(data.accessToken, data.refreshToken);
    return data.accessToken;
  } catch {
    clear();
    return null;
  }
}

function redirectToLogin() {
  if (typeof window === "undefined") return;
  const locale = document.documentElement.lang || "vi";
  window.location.href = `/${locale}/login`;
}

/**
 * Axios instance with interceptors:
 *  - Adds JWT Bearer token from the Zustand auth store
 *  - Adds Accept-Language from the current locale
 *  - On 401: rotates the refresh token once (single-flight) and replays the request;
 *    if refresh fails, clears the session and redirects to login.
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
    if (typeof document !== "undefined") {
      config.headers.set("Accept-Language", document.documentElement.lang || "vi");
    }
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const original = error.config as RetriableConfig | undefined;
      const status = error.response?.status;
      const url = original?.url ?? "";

      // Only attempt a refresh for an authenticated request that 401'd once.
      // Never for the auth endpoints themselves (avoids refresh→refresh loops).
      if (status === 401 && original && !original._retry && !url.includes("/auth/")) {
        original._retry = true;

        refreshPromise = refreshPromise ?? refreshAccessToken(baseURL);
        const newToken = await refreshPromise;
        refreshPromise = null;

        if (newToken) {
          // Replay through the instance so the request interceptor attaches the new token.
          return instance(original);
        }

        redirectToLogin();
      }

      return Promise.reject(error);
    },
  );

  return instance;
}

export const apiClient = createApiClient();
