import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";

export type ResetPasswordRequest = { token: string; newPassword: string };

/** Public password reset — POST /api/v1/auth/reset-password (anonymous, token from the email link). */
export function useResetPasswordMutation() {
  return useMutation({
    mutationFn: async (req: ResetPasswordRequest) => {
      await apiClient.post("/auth/reset-password", req);
    },
  });
}
