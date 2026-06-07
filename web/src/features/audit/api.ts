import { apiClient } from "@/lib/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";

export type AuditLog = {
  id: string;
  actorUserId: string | null;
  actorName: string | null;
  action: string;
  targetEntity: string;
  targetId: string | null;
  ipAddress: string | null;
  metadata: string;
  createdAt: string;
};

/** Admin audit-log search (M16) — GET /api/v1/admin/audit-logs. */
export async function fetchAuditLogs(params: {
  page: number;
  pageSize: number;
  action?: string;
  targetEntity?: string;
}): Promise<PaginatedResponse<AuditLog>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<AuditLog>>>("/admin/audit-logs", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      action: params.action || undefined,
      targetEntity: params.targetEntity || undefined,
    },
  });
  return data.data;
}
