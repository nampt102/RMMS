namespace Rmms.Domain.Enums;

/// <summary>
/// Lifecycle of an <c>approvals</c> row (M09). Stored as snake_case string
/// (<c>pending</c> / <c>approved</c> / <c>rejected</c> / <c>overridden</c>).
/// Simple state machine: Pending → Approved | Rejected | Overridden (BR-401, BR-408).
/// </summary>
public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Overridden = 4,
}
