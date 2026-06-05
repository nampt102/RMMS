import type { UserRole } from "@/types/api";

/** Mirrors `Rmms.Application.Devices.GetPendingDevices.PendingDeviceDto`. */
export type PendingDevice = {
  deviceId: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  userRole: UserRole;
  deviceName: string;
  os: string;
  osVersion: string;
  appVersion: string;
  requestedAt: string;
};
