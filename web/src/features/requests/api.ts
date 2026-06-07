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
