import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";
import type { AdminNewsItem, AssignNewsPayload, NewsPayload } from "./types";

const NEWS_KEY = ["admin", "news"] as const;

/** All news (admin) — GET /api/v1/admin/news. Returns a plain list. */
export async function fetchAdminNews(): Promise<AdminNewsItem[]> {
  const { data } = await apiClient.get<ApiResponse<AdminNewsItem[]>>("/admin/news");
  return data.data;
}

export function useCreateNews() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: NewsPayload) => {
      const { data } = await apiClient.post<ApiResponse<{ id: string }>>("/admin/news", payload);
      return data.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: NEWS_KEY }),
  });
}

export function useUpdateNews() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: NewsPayload }) => {
      await apiClient.patch(`/admin/news/${id}`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: NEWS_KEY }),
  });
}

export function useAssignNews() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: AssignNewsPayload }) => {
      await apiClient.post(`/admin/news/${id}/assignments`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: NEWS_KEY }),
  });
}

export function usePublishNews() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/admin/news/${id}/publish`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: NEWS_KEY }),
  });
}

export function useDeleteNews() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/news/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: NEWS_KEY }),
  });
}
