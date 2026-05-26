import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import { useAuthStore, type AuthUser } from "@/lib/stores/auth-store";
import type { ApiResponse } from "@/types/api";

type LoginRequest = { email: string; password: string };
type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};

/**
 * Authentication mutation — POST /api/v1/auth/login per knowledge-base/05-api-conventions.md.
 * Stores tokens + user in the Zustand auth store on success.
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
      setUser(data.user);
    },
  });
}
