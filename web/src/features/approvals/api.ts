import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type { Approval, EmailActionPreview, EmailActionResult } from "./types";

const APPROVALS_KEY = ["approvals"] as const;

/** Pending approvals routed to the current user (Leader/BUH queue) — GET /approvals/pending. */
export function usePendingApprovals() {
  return useQuery({
    queryKey: [...APPROVALS_KEY, "pending"],
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<Approval[]>>("/approvals/pending");
      return data.data;
    },
  });
}

/** Pending approvals as a plain fetch (for ProTable request) — GET /approvals/pending. */
export async function fetchPendingApprovals(): Promise<Approval[]> {
  const { data } = await apiClient.get<ApiResponse<Approval[]>>("/approvals/pending");
  return data.data;
}

/** Admin: all approvals (optional status filter), paginated — for ProTable request. */
export async function fetchAllApprovals(params: {
  page: number;
  pageSize: number;
  status?: string;
}): Promise<PaginatedResponse<Approval>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<Approval>>>("/admin/approvals", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      status: params.status || undefined,
    },
  });
  return data.data;
}

export function useApproveApproval() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/approvals/${id}/approve`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: APPROVALS_KEY }),
  });
}

export function useRejectApproval() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      await apiClient.post(`/approvals/${id}/reject`, { reason });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: APPROVALS_KEY }),
  });
}

export function useOverrideApproval() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      await apiClient.post(`/admin/approvals/${id}/override`, { reason });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: APPROVALS_KEY }),
  });
}

// ----- Public BUH email-link (no auth — bare axios, never the apiClient) -----

const PUBLIC_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

/** Preview an email-link target without consuming it. */
export async function previewEmailAction(token: string): Promise<EmailActionPreview> {
  const { data } = await axios.get<ApiResponse<EmailActionPreview>>(
    `${PUBLIC_BASE}/api/v1/approvals/email-action`,
    { params: { token } },
  );
  return data.data;
}

/** Submit a BUH decision via the signed link (one-time). */
export async function confirmEmailAction(
  token: string,
  action: "approve" | "reject",
  reason?: string,
): Promise<EmailActionResult> {
  const { data } = await axios.post<ApiResponse<EmailActionResult>>(
    `${PUBLIC_BASE}/api/v1/approvals/email-action/confirm`,
    { token, action, reason },
  );
  return data.data;
}
