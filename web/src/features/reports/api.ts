import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";
import type { AttendanceReportRow, AttendanceTrendPoint, ReportFilters } from "./types";

type ReportKind = "attendance" | "anomalies";

function reportParams(f: ReportFilters) {
  return { from: f.from, to: f.to, storeId: f.storeId || undefined, userId: f.userId || undefined };
}

/** Attendance / anomaly report rows — GET /api/v1/admin/reports/{kind}. */
export async function fetchAttendanceReport(kind: ReportKind, f: ReportFilters): Promise<AttendanceReportRow[]> {
  const { data } = await apiClient.get<ApiResponse<AttendanceReportRow[]>>(`/admin/reports/${kind}`, {
    params: reportParams(f),
  });
  return data.data;
}

/** Daily attendance trend for the dashboard chart — GET /api/v1/admin/reports/attendance-trend. */
export function useAttendanceTrend(days = 14) {
  return useQuery({
    queryKey: ["reports", "attendance-trend", days],
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<AttendanceTrendPoint[]>>("/admin/reports/attendance-trend", {
        params: { days },
      });
      return data.data;
    },
  });
}

/** Download a report as .xlsx — GET /api/v1/admin/reports/{kind}/export (blob). */
export async function downloadReportExcel(kind: ReportKind, f: ReportFilters): Promise<void> {
  const res = await apiClient.get(`/admin/reports/${kind}/export`, {
    params: reportParams(f),
    responseType: "blob",
  });
  const blob = new Blob([res.data as BlobPart], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `${kind}_${f.from}_${f.to}.xlsx`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
