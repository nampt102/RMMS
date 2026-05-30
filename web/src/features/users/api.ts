import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type { AdminUser, CreateUserPayload, ListUsersParams, UpdateUserPayload } from "./types";

const USERS_KEY = ["admin", "users"] as const;

/**
 * Fetch a page of users — GET /api/v1/admin/users.
 * Used directly by the AntD ProTable `request` (ProTable owns pagination/filter state),
 * so it returns the raw paginated envelope rather than going through TanStack Query.
 */
export async function fetchUsers(params: ListUsersParams): Promise<PaginatedResponse<AdminUser>> {
  const { data } = await apiClient.get<PaginatedResponse<AdminUser>>("/admin/users", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      role: params.role || undefined,
      status: params.status || undefined,
      search: params.search || undefined,
    },
  });
  return data;
}

/** Create a Leader / BUH / Admin (initial password is emailed by the backend). */
export function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateUserPayload) => {
      const { data } = await apiClient.post<ApiResponse<AdminUser>>("/admin/users", payload);
      return data.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

/** Update profile + toggle status — PATCH /api/v1/admin/users/:id. */
export function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: UpdateUserPayload }) => {
      const { data } = await apiClient.patch<ApiResponse<AdminUser>>(`/admin/users/${id}`, payload);
      return data.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

/** Admin-triggered password reset — POST /api/v1/admin/users/:id/reset-password (204). */
export function useResetUserPassword() {
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/admin/users/${id}/reset-password`);
    },
  });
}
