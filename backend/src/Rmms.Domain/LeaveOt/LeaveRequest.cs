using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.LeaveOt;

/// <summary>
/// A leave request (M08): regular (date range, optional partial-day times) or emergency
/// (raised while working, linked to the open attendance). Decisions are driven by the M09
/// approval engine — <see cref="ApprovalId"/> links the routed approval.
/// </summary>
public sealed class LeaveRequest : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public LeaveType LeaveType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public RequestStatus Status { get; private set; }
    public Guid? ApprovalId { get; private set; }
    public Guid? LinkedAttendanceId { get; private set; }

    private LeaveRequest() { } // EF Core

    public static LeaveRequest Create(
        Guid userId, LeaveType leaveType, DateOnly startDate, DateOnly endDate,
        TimeOnly? startTime, TimeOnly? endTime, string reason, Guid? linkedAttendanceId = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (endDate < startDate) throw new ArgumentException("End date must be on/after start date.", nameof(endDate));

        return new LeaveRequest
        {
            UserId = userId,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            StartTime = startTime,
            EndTime = endTime,
            Reason = (reason ?? string.Empty).Trim(),
            Status = RequestStatus.Pending,
            LinkedAttendanceId = linkedAttendanceId,
        };
    }

    public bool IsPending => Status == RequestStatus.Pending;

    public void LinkApproval(Guid approvalId) => ApprovalId = approvalId;

    public void Approve(DateTimeOffset now)
    {
        EnsurePending();
        Status = RequestStatus.Approved;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsurePending();
        Status = RequestStatus.Rejected;
        UpdatedAt = now;
    }

    private void EnsurePending()
    {
        if (Status != RequestStatus.Pending)
            throw new InvalidOperationException("Only a pending leave request can be decided.");
    }
}
