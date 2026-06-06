import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import { fetchStores } from "@/features/organization/api";
import { fetchUsers } from "@/features/users/api";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type { AttendanceRecord, ListAttendanceParams } from "./types";

const ATTENDANCE_KEY = ["admin", "attendance"] as const;

/** Paginated attendance — GET /api/v1/admin/attendance. Used directly by ProTable `request`. */
export async function fetchAttendance(
  params: ListAttendanceParams,
): Promise<PaginatedResponse<AttendanceRecord>> {
  const { data } = await apiClient.get<ApiResponse<PaginatedResponse<AttendanceRecord>>>(
    "/admin/attendance",
    {
      params: {
        page: params.page,
        pageSize: params.pageSize,
        userId: params.userId || undefined,
        storeId: params.storeId || undefined,
        status: params.status || undefined,
        from: params.from || undefined,
        to: params.to || undefined,
      },
    },
  );
  return data.data;
}

/** PG + Leader users for the filter dropdown. */
export function useAttendanceUsers() {
  return useQuery({
    queryKey: [...ATTENDANCE_KEY, "users"],
    queryFn: async () => {
      const res = await fetchUsers({ page: 1, pageSize: 200 });
      return res.data.filter((u) => u.role === "pg" || u.role === "leader");
    },
  });
}

/** Stores for the filter dropdown. */
export function useAttendanceStores() {
  return useQuery({
    queryKey: [...ATTENDANCE_KEY, "stores"],
    queryFn: async () => {
      const res = await fetchStores({ page: 1, pageSize: 500 });
      return res.data;
    },
  });
}

/** Approve / reject a pending-review attendance — POST /api/v1/admin/attendance/:id/review. */
export function useReviewAttendance() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, approve, note }: { id: string; approve: boolean; note?: string }) => {
      await apiClient.post(`/admin/attendance/${id}/review`, { approve, note });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ATTENDANCE_KEY }),
  });
}
