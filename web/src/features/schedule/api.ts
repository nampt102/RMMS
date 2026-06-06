import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import { fetchUsers } from "@/features/users/api";
import type { AdminUser } from "@/features/users/types";
import type { ApiResponse } from "@/types/api";
import type { WorkSchedule } from "./types";

const SCHEDULE_KEY = ["admin", "schedule"] as const;

/** PG + Leader users for the schedule picker (Admin can review either). */
export function useSchedulableUsers() {
  return useQuery({
    queryKey: [...SCHEDULE_KEY, "users"],
    queryFn: async () => {
      const res = await fetchUsers({ page: 1, pageSize: 200 });
      return res.data.filter((u: AdminUser) => u.role === "pg" || u.role === "leader");
    },
  });
}

/** A user's schedules in a date range — GET /api/v1/schedule/user/:userId. */
export function useUserSchedule(userId: string | null, from: string, to: string) {
  return useQuery({
    queryKey: [...SCHEDULE_KEY, userId, from, to],
    enabled: userId !== null,
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<WorkSchedule[]>>(`/schedule/user/${userId}`, {
        params: { from, to },
      });
      return data.data;
    },
  });
}

export function useApproveSchedule(userId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (scheduleId: string) => {
      await apiClient.post(`/schedule/${scheduleId}/approve`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [...SCHEDULE_KEY, userId] }),
  });
}

export function useRejectSchedule(userId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ scheduleId, reason }: { scheduleId: string; reason: string }) => {
      await apiClient.post(`/schedule/${scheduleId}/reject`, { reason });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [...SCHEDULE_KEY, userId] }),
  });
}
