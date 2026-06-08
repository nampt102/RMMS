namespace Rmms.Domain.Enums;

/// <summary>
/// In-app / push / email notification types (M14, stored snake_case).
/// Drives the deep-link target and the channel routing policy (CR-3).
/// Phase 1A wires the approval-related types; the rest are defined for M14 full (1B).
/// </summary>
public enum NotificationType
{
    /// <summary>You are the routed approver and a request is awaiting your decision (CR-2).</summary>
    ApprovalNeeded,

    /// <summary>Your request (schedule / leave / OT) was approved.</summary>
    RequestApproved,

    /// <summary>Your request (schedule / leave / OT) was rejected.</summary>
    RequestRejected,

    /// <summary>A PG device change is awaiting approval.</summary>
    DeviceChangeRequest,

    /// <summary>One of your attendance records was sent to Admin Review (in-app only).</summary>
    AttendanceInReview,

    /// <summary>A new / important news item was published to you (1B).</summary>
    News,

    /// <summary>A new document is available (1B).</summary>
    Document,

    /// <summary>A new payslip is available (1B).</summary>
    Payslip,

    /// <summary>A form deadline is approaching or has passed (1B).</summary>
    FormDeadline,

    /// <summary>Generic / fallback notification.</summary>
    General,
}
