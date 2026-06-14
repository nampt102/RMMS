/** Mirrors `Rmms.Application.Reports.*` DTOs (camelCase over the wire). */

export type AttendanceReportRow = {
  date: string; // ISO date (yyyy-MM-dd)
  userName: string;
  storeName: string;
  checkInAt: string;
  checkOutAt: string | null;
  status: string;
  isAnomaly: boolean;
};

export type AttendanceTrendPoint = {
  date: string;
  valid: number;
  late: number;
  anomaly: number;
};

export type ReportFilters = {
  from: string; // yyyy-MM-dd
  to: string; // yyyy-MM-dd
  storeId?: string;
  userId?: string;
};
