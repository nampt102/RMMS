using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.LeaveOt;

/// <summary>
/// An overtime request (M08): a date + start/end time + reason. Decisions are driven by
/// the M09 approval engine — <see cref="ApprovalId"/> links the routed approval.
/// </summary>
public sealed class OtRequest : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public DateOnly OtDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public RequestStatus Status { get; private set; }
    public Guid? ApprovalId { get; private set; }

    private OtRequest() { } // EF Core

    public static OtRequest Create(Guid userId, DateOnly otDate, TimeOnly startTime, TimeOnly endTime, string reason)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (endTime <= startTime) throw new ArgumentException("End time must be after start time.", nameof(endTime));

        return new OtRequest
        {
            UserId = userId,
            OtDate = otDate,
            StartTime = startTime,
            EndTime = endTime,
            Reason = (reason ?? string.Empty).Trim(),
            Status = RequestStatus.Pending,
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
            throw new InvalidOperationException("Only a pending OT request can be decided.");
    }
}
