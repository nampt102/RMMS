/** Mirrors `Rmms.Application.Attendance.AttendanceDto` (camelCase over the wire). */

export type AttendanceStatus =
  | "valid"
  | "late"
  | "gps_violation_pending_review"
  | "face_fail_pending_review"
  | "fake_gps_blocked"
  | "admin_approved"
  | "admin_rejected";

export type FaceResult = "success" | "fail" | "pending_review";

export type AttendanceRecord = {
  id: string;
  userId: string;
  workScheduleShiftId: string;
  storeId: string;
  storeCode: string;
  storeName: string;
  status: AttendanceStatus;
  isLate: boolean;
  // check-in
  checkInAt: string;
  checkInLatitude: number;
  checkInLongitude: number;
  checkInDistanceMeters: number;
  checkInFaceResult: FaceResult;
  checkInFaceConfidence: number | null;
  checkInSelfieUrl: string | null;
  checkInStorePhotoUrl: string | null;
  checkInFakeGpsDetected: boolean;
  checkInNote: string | null;
  // check-out (nullable)
  checkOutAt: string | null;
  checkOutLatitude: number | null;
  checkOutLongitude: number | null;
  checkOutDistanceMeters: number | null;
  checkOutFaceResult: FaceResult | null;
  checkOutFaceConfidence: number | null;
  checkOutSelfieUrl: string | null;
  checkOutStorePhotoUrl: string | null;
  checkOutNote: string | null;
  // review
  reviewedBy: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
};

export type ListAttendanceParams = {
  page: number;
  pageSize: number;
  userId?: string;
  storeId?: string;
  status?: string;
  from?: string;
  to?: string;
};

/** Statuses that sit in the Admin review queue and can be approved/rejected. */
export const REVIEWABLE_STATUSES: AttendanceStatus[] = [
  "gps_violation_pending_review",
  "face_fail_pending_review",
];
