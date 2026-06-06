/** Mirrors `Rmms.Application.Scheduling.*` DTOs (camelCase over the wire). */

export type ScheduleStatus =
  | "pending"
  | "approved"
  | "rejected"
  | "edit_pending"
  | "superseded";

export type WorkScheduleShift = {
  id: string;
  storeId: string;
  storeCode: string;
  storeName: string;
  startTime: string; // "HH:mm"
  endTime: string; // "HH:mm"
  ordering: number;
};

export type WorkSchedule = {
  id: string;
  userId: string;
  scheduleDate: string; // "YYYY-MM-DD"
  status: ScheduleStatus;
  version: number;
  previousVersionId: string | null;
  submittedAt: string | null;
  approvedAt: string | null;
  rejectReason: string | null;
  shifts: WorkScheduleShift[];
};
