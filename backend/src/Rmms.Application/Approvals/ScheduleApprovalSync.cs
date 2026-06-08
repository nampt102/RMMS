using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;
using Rmms.Domain.Notifications;
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
    /// <summary>
    /// M09 → entity: apply an approval decision to the linked request/schedule, then notify
    /// the requester (CR-2: schedule/leave/OT approved or rejected → in-app + push + email, CR-3).
    /// The in-app row is added to the same unit of work; push/email are best-effort.
    /// </summary>
    public static async Task ApplyDecisionAsync(
        IAppDbContext db, INotificationService notifier, Approval approval, bool approve, string? reason, Guid actorId, DateTimeOffset now, CancellationToken ct)
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

        await notifier.NotifyAsync(approval.RequesterId, BuildDecisionSpec(approval.EntityType, approval.EntityId, approve, reason), ct);
    }

    /// <summary>
    /// Bilingual "your request was approved/rejected" notification (CR-2/CR-3). Public so the
    /// M07 schedule-decision surface can reuse it when a decision is made there directly.
    /// </summary>
    public static NotificationSpec BuildDecisionSpec(ApprovalEntityType entityType, Guid entityId, bool approve, string? reason)
    {
        var (kindVi, kindEn) = entityType switch
        {
            ApprovalEntityType.WorkSchedule => ("Lịch làm việc", "Work schedule"),
            ApprovalEntityType.LeaveRequest => ("Đơn nghỉ phép", "Leave request"),
            ApprovalEntityType.OtRequest => ("Đơn tăng ca", "OT request"),
            _ => ("Yêu cầu", "Request"),
        };
        var deepLink = entityType == ApprovalEntityType.WorkSchedule
            ? $"rmms://schedules/{entityId}"
            : $"rmms://requests/{entityId}";

        var data = new Dictionary<string, string>
        {
            ["deepLink"] = deepLink,
            ["entityType"] = entityType.ToSnakeCase(),
            ["entityId"] = entityId.ToString(),
        };

        if (approve)
        {
            return new NotificationSpec(
                NotificationType.RequestApproved,
                TitleVi: $"{kindVi} đã được duyệt",
                TitleEn: $"{kindEn} approved",
                BodyVi: $"{kindVi} của bạn đã được phê duyệt.",
                BodyEn: $"Your {kindEn.ToLowerInvariant()} has been approved.",
                Data: data, Push: true, Email: true);
        }

        var why = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return new NotificationSpec(
            NotificationType.RequestRejected,
            TitleVi: $"{kindVi} bị từ chối",
            TitleEn: $"{kindEn} rejected",
            BodyVi: why is null ? $"{kindVi} của bạn đã bị từ chối." : $"{kindVi} của bạn đã bị từ chối. Lý do: {why}",
            BodyEn: why is null ? $"Your {kindEn.ToLowerInvariant()} was rejected." : $"Your {kindEn.ToLowerInvariant()} was rejected. Reason: {why}",
            Data: data, Push: true, Email: true);
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
