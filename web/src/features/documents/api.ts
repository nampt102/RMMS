import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";
import type { AssignDocumentPayload, DocumentItem } from "./types";

const DOCS_KEY = ["admin", "documents"] as const;

/** All documents (admin) — GET /api/v1/admin/documents. Returns a plain list. */
export async function fetchAdminDocuments(folderType?: string): Promise<DocumentItem[]> {
  const { data } = await apiClient.get<ApiResponse<DocumentItem[]>>("/admin/documents", {
    params: { folderType: folderType || undefined },
  });
  return data.data;
}

/** Upload a document (multipart) — POST /api/v1/admin/documents. */
export function useUploadDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: { name: string; description?: string; folderType: string; file: File }) => {
      const form = new FormData();
      form.append("name", input.name);
      if (input.description) form.append("description", input.description);
      form.append("folderType", input.folderType);
      form.append("file", input.file);
      await apiClient.post("/admin/documents", form, {
        headers: { "Content-Type": "multipart/form-data" },
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: DOCS_KEY }),
  });
}

/** Grant access by role or single user — POST /api/v1/admin/documents/:id/assignments. */
export function useAssignDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: AssignDocumentPayload }) => {
      await apiClient.post(`/admin/documents/${id}/assignments`, payload);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: DOCS_KEY }),
  });
}

export function useDeleteDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/documents/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: DOCS_KEY }),
  });
}
