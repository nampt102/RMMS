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

    // (M03+ actions appended here as modules ship)
}
