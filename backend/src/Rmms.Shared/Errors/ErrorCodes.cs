namespace Rmms.Shared.Errors;

/// <summary>
/// Catalogue of domain error codes per <c>knowledge-base/05-api-conventions.md</c>.
/// Use these constants throughout — never magic strings.
/// </summary>
public static class ErrorCodes
{
    // ---------- Generic ----------
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string UpstreamUnavailable = "UPSTREAM_UNAVAILABLE";

    // ---------- Auth ----------
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string RefreshTokenRevoked = "REFRESH_TOKEN_REVOKED";
    public const string RefreshTokenReused = "REFRESH_TOKEN_REUSED";
    public const string DeviceNotAuthorized = "DEVICE_NOT_AUTHORIZED";
    public const string PasswordTooWeak = "PASSWORD_TOO_WEAK";
    public const string PermissionDenied = "PERMISSION_DENIED";

    // ---------- Attendance ----------
    public const string StoreNotAssigned = "STORE_NOT_ASSIGNED";
    public const string FakeGpsDetected = "FAKE_GPS_DETECTED";
    public const string FaceVerificationFailed = "FACE_VERIFICATION_FAILED";
    public const string FaceNotEnrolled = "FACE_NOT_ENROLLED";
    public const string AlreadyCheckedIn = "ALREADY_CHECKED_IN";
    public const string NoOpenAttendance = "NO_OPEN_ATTENDANCE";
    public const string CheckInTooEarly = "CHECK_IN_TOO_EARLY";
    public const string ShiftNotFound = "SHIFT_NOT_FOUND";

    // ---------- Approval ----------
    public const string ApprovalNotPending = "APPROVAL_NOT_PENDING";
    public const string NotApprover = "NOT_APPROVER";
    public const string RejectReasonRequired = "REJECT_REASON_REQUIRED";
    public const string EmailTokenExpired = "EMAIL_TOKEN_EXPIRED";
    public const string EmailTokenUsed = "EMAIL_TOKEN_USED";

    // ---------- Organization & Assignment (M03) ----------
    public const string CodeAlreadyExists = "CODE_ALREADY_EXISTS";
    public const string InvalidReference = "INVALID_REFERENCE";
    public const string InvalidAssignment = "INVALID_ASSIGNMENT";
    public const string AssignmentExists = "ASSIGNMENT_EXISTS";

    // ---------- Form Engine ----------
    public const string FormNotAssigned = "FORM_NOT_ASSIGNED";
    public const string FormExpired = "FORM_EXPIRED";
    public const string FormDeadlinePassed = "FORM_DEADLINE_PASSED";
    public const string EditNotAllowed = "EDIT_NOT_ALLOWED";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
}
