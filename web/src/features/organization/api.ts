import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type {
  Area,
  Category,
  CreateStorePayload,
  ListStoresParams,
  Store,
  UpdateStorePayload,
} from "./types";

const STORES_KEY = ["admin", "stores"] as const;
const AREAS_KEY = ["admin", "areas"] as const;
const CATEGORIES_KEY = ["admin", "categories"] as const;

// ---------------- Stores ----------------

/** Paginated stores — GET /api/v1/admin/stores. Used directly by ProTable `request`. */
export async function fetchStores(params: ListStoresParams): Promise<PaginatedResponse<Store>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<Store>>>("/admin/stores", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      areaId: params.areaId || undefined,
      status: params.status || undefined,
      search: params.search || undefined,
    },
  });
  return data.data;
}

export function useCreateStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateStorePayload) => {
      await apiClient.post("/admin/stores", payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: STORES_KEY }),
  });
}

export function useUpdateStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: UpdateStorePayload }) => {
      await apiClient.patch(`/admin/stores/${id}`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: STORES_KEY }),
  });
}

export function useChangeStoreStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: "active" | "inactive" }) => {
      await apiClient.post(`/admin/stores/${id}/status`, { status });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: STORES_KEY }),
  });
}

export function useDeleteStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/stores/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: STORES_KEY }),
  });
}

// ---------------- Areas ----------------

async function fetchAreas(): Promise<Area[]> {
  const { data } = await apiClient.get<ApiResponse<Area[]>>("/admin/areas");
  return data.data;
}

/** Areas list via TanStack Query (used for both the Areas page and the Store area dropdown). */
export function useAreas() {
  return useQuery({ queryKey: AREAS_KEY, queryFn: fetchAreas });
}

export function useCreateArea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { code: string; name: string; parentAreaId?: string }) => {
      await apiClient.post("/admin/areas", payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: AREAS_KEY }),
  });
}

export function useUpdateArea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: { name: string; parentAreaId?: string } }) => {
      await apiClient.patch(`/admin/areas/${id}`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: AREAS_KEY }),
  });
}

export function useDeleteArea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/areas/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: AREAS_KEY }),
  });
}

// ---------------- Categories ----------------

async function fetchCategories(): Promise<Category[]> {
  const { data } = await apiClient.get<ApiResponse<Category[]>>("/admin/categories");
  return data.data;
}

export function useCategories() {
  return useQuery({ queryKey: CATEGORIES_KEY, queryFn: fetchCategories });
}

export function useCreateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { code: string; name: string }) => {
      await apiClient.post("/admin/categories", payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: CATEGORIES_KEY }),
  });
}

export function useUpdateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: { name: string } }) => {
      await apiClient.patch(`/admin/categories/${id}`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: CATEGORIES_KEY }),
  });
}

export function useDeleteCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/categories/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: CATEGORIES_KEY }),
  });
}
