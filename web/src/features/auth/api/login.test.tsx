import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/client";
import { useLoginMutation } from "@/features/auth/api/login";
import { useAuthStore } from "@/lib/stores/auth-store";

vi.mock("@/lib/api/client", () => ({
  apiClient: { post: vi.fn() },
}));

const mockedPost = vi.mocked(apiClient.post);

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

describe("useLoginMutation", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.getState().clear();
  });

  it("persists tokens and maps user.userId -> AuthUser.id on success", async () => {
    mockedPost.mockResolvedValueOnce({
      data: {
        data: {
          accessToken: "access-1",
          accessTokenExpiresAt: "2026-06-04T00:15:00Z",
          refreshToken: "refresh-1",
          refreshTokenExpiresAt: "2026-07-04T00:00:00Z",
          user: {
            userId: "u-123",
            email: "admin@rmms.local",
            fullName: "System Admin",
            role: "admin",
            status: "active",
            preferredLanguage: "vi",
          },
        },
      },
    });

    const { result } = renderHook(() => useLoginMutation(), { wrapper });

    await result.current.mutateAsync({ email: "admin@rmms.local", password: "Admin123" });

    expect(mockedPost).toHaveBeenCalledWith("/auth/login", {
      email: "admin@rmms.local",
      password: "Admin123",
    });

    const state = useAuthStore.getState();
    expect(state.accessToken).toBe("access-1");
    expect(state.refreshToken).toBe("refresh-1");
    expect(state.user).toEqual({
      id: "u-123",
      email: "admin@rmms.local",
      fullName: "System Admin",
      role: "admin",
    });
  });

  it("does not store tokens when the request fails", async () => {
    mockedPost.mockRejectedValueOnce(new Error("401"));

    const { result } = renderHook(() => useLoginMutation(), { wrapper });

    await expect(
      result.current.mutateAsync({ email: "x@y.z", password: "bad" }),
    ).rejects.toThrow();

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(useAuthStore.getState().accessToken).toBeNull();
  });
});
