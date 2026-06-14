import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type { CreateProductPayload, ListProductsParams, Product, UpdateProductPayload } from "./types";

const PRODUCTS_KEY = ["admin", "products"] as const;

/** Paginated products — GET /api/v1/admin/products. Used directly by ProTable `request`. */
export async function fetchProducts(params: ListProductsParams): Promise<PaginatedResponse<Product>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<Product>>>("/admin/products", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      categoryId: params.categoryId || undefined,
      search: params.search || undefined,
    },
  });
  return data.data;
}

export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateProductPayload) => {
      await apiClient.post("/admin/products", payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: PRODUCTS_KEY }),
  });
}

export function useUpdateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: UpdateProductPayload }) => {
      await apiClient.patch(`/admin/products/${id}`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: PRODUCTS_KEY }),
  });
}

export function useChangeProductStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: "active" | "inactive" }) => {
      await apiClient.post(`/admin/products/${id}/status`, { status });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: PRODUCTS_KEY }),
  });
}

export function useDeleteProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/products/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: PRODUCTS_KEY }),
  });
}
