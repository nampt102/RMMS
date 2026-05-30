import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { ApiResponse, UserRole } from "@/types/api";

export type LoginRequest = { email: string; password: string };

/**
 * `data.user` shape returned by POST /api/v1/auth/login.
 * Mirrors `Rmms.Application.Auth.Login.LoginUserInfo` (camelCase over the wire).
 */
type LoginUserDto = {
  userId: string;
  email: string;
  fullName: string;
  role: UserRole;
  status: string;
  preferredLanguage: string;
};

type LoginResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: LoginUserDto;
};

/**
 * Authentication mutation — POST /api/v1/auth/login per knowledge-base/05-api-conventions.md.
 *
 * Web users are Leader / BUH / Admin. BR-105's single-device rule is PG-scoped, so the web
 * client sends NO device payload (the backend only requires `device` for PG / mobile).
 *
 * Stores tokens + user in the Zustand auth store on success. Errors propagate as AxiosError —
 * the caller reads `error.response.data.error.code` to localize the message.
 */
export function useLoginMutation() {
  const setTokens = useAuthStore((s) => s.setTokens);
  const setUser = useAuthStore((s) => s.setUser);

  return useMutation({
    mutationFn: async (payload: LoginRequest) => {
      const { data } = await apiClient.post<ApiResponse<LoginResponse>>("/auth/login", payload);
      return data.data;
    },
    onSuccess: (data) => {
      setTokens(data.accessToken, data.refreshToken);
      setUser({
        id: data.user.userId,
        email: data.user.email,
        fullName: data.user.fullName,
        role: data.user.role,
      });
    },
  });
}
