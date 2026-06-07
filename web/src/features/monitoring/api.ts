import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";

export type TeamMemberStatus = {
  userId: string;
  fullName: string;
  role: string;
  status: string;
  checkInAt: string | null;
  storeName: string | null;
};

export type TeamToday = {
  members: TeamMemberStatus[];
  summary: Record<string, number>;
  asOf: string;
};

/** Today's team status snapshot (M12) — GET /api/v1/team-monitoring/today. */
export function useTeamToday() {
  return useQuery({
    queryKey: ["team-monitoring", "today"],
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<TeamToday>>("/team-monitoring/today");
      return data.data;
    },
  });
}
