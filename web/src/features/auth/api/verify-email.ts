import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";

/** Public email verification — POST /api/v1/auth/verify-email (anonymous, token from the email link). */
export function useVerifyEmailMutation() {
  return useMutation({
    mutationFn: async (token: string) => {
      await apiClient.post("/auth/verify-email", { token });
    },
  });
}
