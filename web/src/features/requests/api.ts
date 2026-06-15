import { apiClient } from "@/lib/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type { LeaveRequest, OtRequest } from "./types";

/** Admin: all leave requests (optional status filter), paginated — for ProTable request. */
export async function fetchAllLeaveRequests(params: {
  page: number;
  pageSize: number;
  status?: string;
}): Promise<PaginatedResponse<LeaveRequest>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<LeaveRequest>>>("/admin/leave-requests", {
    params: { page: params.page, pageSize: params.pageSize, status: params.status || undefined },
  });
  return data.data;
}

/** Admin: all OT requests (optional status filter), paginated — for ProTable request. */
export async function fetchAllOtRequests(params: {
  page: number;
  pageSize: number;
  status?: string;
}): Promise<PaginatedResponse<OtRequest>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<OtRequest>>>("/admin/ot-requests", {
    params: { page: params.page, pageSize: params.pageSize, status: params.status || undefined },
  });
  return data.data;
}

/** Download a leave/OT report as .xlsx — GET /api/v1/admin/{kind}-requests/export (blob). */
export async function downloadRequestsExcel(kind: "leave" | "ot", status?: string): Promise<void> {
  const res = await apiClient.get(`/admin/${kind}-requests/export`, {
    params: { status: status || undefined },
    responseType: "blob",
  });
  const blob = new Blob([res.data as BlobPart], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `${kind}-requests.xlsx`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
