using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Domain.Scheduling;

namespace Rmms.Application.Approvals;

/// <summary>
/// Keeps the generic M09 <c>approvals</c> row and its underlying <c>work_schedule</c>
/// consistent regardless of which surface made the decision — the M07 schedule
/// endpoints (web <c>/schedules</c>) or the M09 queue / BUH email-link. Each direction
/// only mutates the other side when it is still pending, so the two never loop and a
/// decision made on either surface clears the other.
/// </summary>
internal static class ScheduleApprovalSync
{
    /// <summary>M09 → M07: apply an approval decision to the linked work schedule (BR-308 supersede on approve).</summary>
    public static async Task ApplyToScheduleAsync(
        IAppDbContext db, Approval approval, bool approve, string? reason, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        if (approval.EntityType != ApprovalEntityType.WorkSchedule) return;

        var schedule = await db.WorkSchedules.FirstOrDefaultAsync(s => s.Id == approval.EntityId, ct);
        if (schedule is null) return;
        if (schedule.Status is not (WorkScheduleStatus.Pending or WorkScheduleStatus.EditPending)) return;

        if (approve)
        {
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
    public static async Task SyncApprovalAsync(
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
