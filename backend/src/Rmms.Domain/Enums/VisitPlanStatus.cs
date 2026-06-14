namespace Rmms.Domain.Enums;

/// <summary>
/// Lifecycle of a Visit Plan (M11). Stored as snake_case string.
///   - <c>Pending</c>: created by a Leader, awaiting BUH approval (M09).
///   - <c>Approved</c> / <c>Rejected</c>: BUH decision applied (via web or email link, BR-407).
///   - <c>Executed</c>: every planned item has a linked post-visit form submission.
/// </summary>
public enum VisitPlanStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Executed = 4,
}
