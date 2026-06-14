import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";
import type { ListVisitPlansParams, VisitPlan } from "./types";

/** All visit plans (admin, read-only) — GET /api/v1/admin/visit-plans. Returns a plain list. */
export async function fetchAdminVisitPlans(params: ListVisitPlansParams): Promise<VisitPlan[]> {
  const { data } = await apiClient.get<ApiResponse<VisitPlan[]>>("/admin/visit-plans", {
    params: {
      status: params.status || undefined,
      leaderUserId: params.leaderUserId || undefined,
    },
  });
  return data.data;
}
