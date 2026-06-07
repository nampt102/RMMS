namespace Rmms.Domain.Enums;

/// <summary>
/// Lifecycle of a leave / OT request (M08). Stored as snake_case string
/// (<c>pending</c> / <c>approved</c> / <c>rejected</c>). Driven by the M09 approval engine.
/// </summary>
public enum RequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
}
