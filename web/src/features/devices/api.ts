import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";
import type { PendingDevice } from "./types";

/**
 * Pending device-change requests — GET /api/v1/devices/pending.
 * Returns a plain list value (single `{ data: [...] }` envelope, not paginated).
 */
export async function fetchPendingDevices(): Promise<PendingDevice[]> {
  const { data } = await apiClient.get<ApiResponse<PendingDevice[]>>("/devices/pending");
  return data.data;
}

/** Approve a pending device-change request — POST /api/v1/devices/:id/approve (204). */
export function useApproveDevice() {
  return useMutation({
    mutationFn: async (deviceId: string) => {
      await apiClient.post(`/devices/${deviceId}/approve`);
    },
  });
}

/** Reject a pending device-change request — POST /api/v1/devices/:id/reject (204). */
export function useRejectDevice() {
  return useMutation({
    mutationFn: async ({ deviceId, reason }: { deviceId: string; reason: string }) => {
      await apiClient.post(`/devices/${deviceId}/reject`, { reason });
    },
  });
}
