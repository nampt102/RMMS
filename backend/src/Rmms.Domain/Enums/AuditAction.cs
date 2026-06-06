namespace Rmms.Domain.Enums;

/// <summary>
/// Canonical audit action codes per <c>06-business-rules.md</c> CR-1.
/// Stored as <c>varchar</c> in <c>audit_log.action</c> column to allow extension
/// without schema change. Using string constants (not enum) gives flexibility
/// for module-specific actions added later (e.g., M10 form.publish).
///
/// Convention: <c>{aggregate}.{verb_past}</c>, lowercase, snake_case.
/// </summary>
public static class AuditAction
{
    // ----- M01 Identity & Access -----
    public const string UserRegistered = "user.registered";
    public const string UserEmailVerified = "user.email_verified";
    public const string UserCreatedByAdmin = "user.created_by_admin";
    public const string UserUpdatedByAdmin = "user.updated_by_admin";
    public const string UserStatusChanged = "user.status_changed";
    public const string UserPasswordReset = "user.password_reset";
    public const string UserPasswordResetRequested = "user.password_reset_requested";

    public const string AuthLoginSuccess = "auth.login_success";
    public const string AuthLoginFailed = "auth.login_failed";
    public const string AuthLogout = "auth.logout";
    public const string AuthRefreshRotated = "auth.refresh_rotated";
    public const string AuthRefreshReused = "auth.refresh_reused"; // security incident

    // ----- M02 Device Management -----
    public const string DeviceRegistered = "device.registered";
    public const string DeviceChangeRequested = "device.change_requested";
    public const string DeviceApproved = "device.approved";
    public const string DeviceRejected = "device.rejected";

    // ----- M03 Organization & Assignment -----
    public const string StoreCreated = "store.created";
    public const string StoreUpdated = "store.updated";
    public const string StoreStatusChanged = "store.status_changed";
    public const string StoreDeleted = "store.deleted";

    public const string AreaCreated = "area.created";
    public const string AreaUpdated = "area.updated";
    public const string AreaDeleted = "area.deleted";

    public const string CategoryCreated = "category.created";
    public const string CategoryUpdated = "category.updated";
    public const string CategoryDeleted = "category.deleted";

    public const string PgLeaderAssigned = "assignment.pg_leader_assigned";
    public const string UserStoreAssigned = "assignment.user_store_assigned";
    public const string UserStoreUnassigned = "assignment.user_store_unassigned";
    public const string UserCategoryAssigned = "assignment.user_category_assigned";
    public const string UserCategoryUnassigned = "assignment.user_category_unassigned";

    // ----- M07 Work Schedule -----
    public const string ScheduleCreated = "schedule.created";
    public const string ScheduleSubmitted = "schedule.submitted";
    public const string ScheduleEdited = "schedule.edited";
    public const string ScheduleWithdrawn = "schedule.withdrawn";
    public const string ScheduleApproved = "schedule.approved";
    public const string ScheduleRejected = "schedule.rejected";

    // ----- M05 Attendance (CR-1) -----
    public const string AttendanceCheckedIn = "attendance.checked_in";
    public const string AttendanceCheckedOut = "attendance.checked_out";
    public const string AttendanceFaceFailed = "attendance.face_failed";
    public const string AttendanceGpsViolation = "attendance.gps_violation";
    public const string AttendanceFakeGpsBlocked = "attendance.fake_gps_blocked";
    public const string AttendanceReviewed = "attendance.reviewed";

    // ----- M06 Face Verification (CR-1) -----
    public const string FaceEnrolled = "face.enrolled";
    public const string FaceRemoved = "face.removed"; // admin remove / re-enroll trigger

    // ----- M09 Approval Workflow (CR-1) -----
    public const string ApprovalRequested = "approval.requested";
    public const string ApprovalApproved = "approval.approved";
    public const string ApprovalRejected = "approval.rejected";
    public const string ApprovalOverridden = "approval.overridden"; // Admin override (BR-408)

    // (M04+ actions appended here as modules ship)
}
