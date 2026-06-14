import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";
import type {
  CreateFormPayload,
  FormDetail,
  FormSummary,
  FormVersionInfo,
  UpdateFormPayload,
} from "./types";

const FORMS_KEY = ["admin", "forms"] as const;

export function useForms() {
  return useQuery({
    queryKey: FORMS_KEY,
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<FormSummary[]>>("/admin/forms");
      return data.data;
    },
  });
}

export function useForm(id: string | undefined) {
  return useQuery({
    queryKey: [...FORMS_KEY, id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<FormDetail>>(`/admin/forms/${id}`);
      return data.data;
    },
  });
}

export function useFormVersions(id: string | undefined) {
  return useQuery({
    queryKey: [...FORMS_KEY, id, "versions"],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<FormVersionInfo[]>>(`/admin/forms/${id}/versions`);
      return data.data;
    },
  });
}

export function useCreateForm() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateFormPayload) => {
      const { data } = await apiClient.post<ApiResponse<{ id: string }>>("/admin/forms", payload);
      return data.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: FORMS_KEY }),
  });
}

export function useUpdateForm() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: UpdateFormPayload }) => {
      await apiClient.patch(`/admin/forms/${id}`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: FORMS_KEY }),
  });
}

export function usePublishForm() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.post<ApiResponse<{ version: number }>>(`/admin/forms/${id}/publish`, {});
      return data.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: FORMS_KEY }),
  });
}
