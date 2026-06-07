/** Mirrors `Rmms.Application.LeaveOt` DTOs (M08, camelCase over the wire). */
export type RequestStatus = "pending" | "approved" | "rejected";

export type LeaveRequest = {
  id: string;
  userId: string;
  leaveType: string; // regular | emergency
  startDate: string;
  endDate: string;
  startTime: string | null;
  endTime: string | null;
  reason: string;
  status: RequestStatus;
  approvalId: string | null;
  linkedAttendanceId: string | null;
  createdAt: string;
  requesterName: string | null;
};

export type OtRequest = {
  id: string;
  userId: string;
  otDate: string;
  startTime: string;
  endTime: string;
  reason: string;
  status: RequestStatus;
  approvalId: string | null;
  createdAt: string;
  requesterName: string | null;
};
