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
  // The API wraps every success body in `{ data: ... }` (see ResultMapping.Ok), and the
  // paginated payload is itself `{ data: [...], meta: {...} }` — so the wire shape is
  // `{ data: { data: [...], meta: {...} } }`. Unwrap the outer envelope here.
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<AdminUser>>>("/admin/users", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      role: params.role || undefined,
      status: params.status || undefined,
      search: params.search || undefined,
    },
  });
  return data.data;
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

/**
 * Force a user to re-enroll their face (M06) — POST /api/v1/admin/face/re-enroll/:id (204).
 * Clears the current enrollment so the user must re-capture on next login.
 */
export function useReEnrollFace() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/admin/face/re-enroll/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

/**
 * Enroll a user's face on their behalf (M06) — POST /api/v1/admin/face/enroll/:id (multipart).
 * Sends 1..5 face photos; replaces any prior enrollment server-side.
 */
export function useAdminEnrollFace() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, files }: { id: string; files: File[] }) => {
      const form = new FormData();
      for (const file of files) form.append("photos", file);
      await apiClient.post(`/admin/face/enroll/${id}`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

/** Remove a user's face template entirely — DELETE /api/v1/admin/face/template/:id (204). */
export function useRemoveFace() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/face/template/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}
