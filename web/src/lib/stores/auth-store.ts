import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { UserRole } from "@/types/api";

export type AuthUser = {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
};

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  setTokens: (access: string, refresh: string) => void;
  setUser: (user: AuthUser | null) => void;
  clear: () => void;
};

/**
 * Auth store — persisted to localStorage so refresh survives page reloads.
 * Refresh token rotation lives in `lib/api/client.ts` interceptors.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setTokens: (access, refresh) => set({ accessToken: access, refreshToken: refresh }),
      setUser: (user) => set({ user }),
      clear: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: "rmms-auth",
      version: 1,
    },
  ),
);
