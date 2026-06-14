import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";

export type DashboardSummary = {
  totalMembers: number;
  online: number;
  checkedOutToday: number;
  notCheckedIn: number;
  onLeave: number;
  pendingReviewAttendance: number;
  pendingApprovals: number;
  anomaliesToday: number;
  asOf: string;
};

/** KPI summary for the dashboard landing (M15) — GET /api/v1/admin/dashboard/summary. */
export function useDashboardSummary() {
  return useQuery({
    queryKey: ["dashboard", "summary"],
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<DashboardSummary>>("/admin/dashboard/summary");
      return data.data;
    },
  });
}
