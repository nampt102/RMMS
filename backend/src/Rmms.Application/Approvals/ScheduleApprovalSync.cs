using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;
using Rmms.Domain.Scheduling;

namespace Rmms.Application.Approvals;

/// <summary>
/// Bridges a generic M09 approval decision to the underlying domain entity (work schedule,
/// leave request, OT request) and keeps the two consistent regardless of which surface made
/// the decision. Each direction only mutates the other while it is still pending, so a
/// decision on either side clears the other and there are no loops.
/// </summary>
internal static class ApprovalActuation
{
    /// <summary>M09 → entity: apply an approval decision to the linked request/schedule.</summary>
    public static async Task ApplyDecisionAsync(
        IAppDbContext db, Approval approval, bool approve, string? reason, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        switch (approval.EntityType)
        {
            case ApprovalEntityType.WorkSchedule:
                await ApplyToScheduleAsync(db, approval.EntityId, approve, reason, actorId, now, ct);
                break;

            case ApprovalEntityType.LeaveRequest:
                var leave = await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == approval.EntityId, ct);
                if (leave is { IsPending: true })
                {
                    if (approve) leave.Approve(now); else leave.Reject(now);
                }
                break;

            case ApprovalEntityType.OtRequest:
                var ot = await db.OtRequests.FirstOrDefaultAsync(x => x.Id == approval.EntityId, ct);
                if (ot is { IsPending: true })
                {
                    if (approve) ot.Approve(now); else ot.Reject(now);
                }
                break;
        }
    }

    private static async Task ApplyToScheduleAsync(
        IAppDbContext db, Guid scheduleId, bool approve, string? reason, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        var schedule = await db.WorkSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
        if (schedule is null) return;
        if (schedule.Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending)) return;

        if (approve)
        {
            // BR-308: approving an edit supersedes the still-effective old approved version.
            if (schedule.Status == WorkScheduleStatus.EditPending && schedule.PreviousVersionId is { } prevId)
            {
                var previous = await db.WorkSchedules
                    .FirstOrDefaultAsync(s => s.Id == prevId && s.Status == WorkScheduleStatus.Approved, ct);
                previous?.Supersede();
            }
            schedule.Approve(actorId, now);
        }
        else
        {
            schedule.Reject(actorId, string.IsNullOrWhiteSpace(reason) ? "—" : reason, now);
        }
    }

    /// <summary>M07 → M09: mark the schedule's pending approval decided so the queue clears.</summary>
    public static async Task SyncScheduleApprovalAsync(
        IAppDbContext db, Guid scheduleId, bool approve, string? reason, Guid actorId, ApprovalDecisionVia via, DateTimeOffset now, CancellationToken ct)
    {
        var approval = await db.Approvals.FirstOrDefaultAsync(
            a => a.EntityType == ApprovalEntityType.WorkSchedule
              && a.EntityId == scheduleId
              && a.Status == ApprovalStatus.Pending, ct);
        if (approval is null) return;

        if (approve) approval.Approve(actorId, via, now);
        else approval.Reject(actorId, string.IsNullOrWhiteSpace(reason) ? "—" : reason, via, now);
    }
}
