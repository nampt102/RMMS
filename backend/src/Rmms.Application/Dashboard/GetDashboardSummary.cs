using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.Dashboard;

/// <summary>
/// Top-of-dashboard KPIs (M15 Phase 1A basic, AC-27). Kept intentionally small: today's
/// presence (online / checked-out / not-checked-in / on-leave) plus the three actionable
/// backlogs (attendance pending review, pending approvals, today's anomalies).
/// </summary>
public sealed record DashboardSummaryDto(
    int TotalMembers,
    int Online,
    int CheckedOutToday,
    int NotCheckedIn,
    int OnLeave,
    int PendingReviewAttendance,
    int PendingApprovals,
    int AnomaliesToday,
    DateTimeOffset AsOf);

/// <summary>
/// Scope mirrors M12 team monitoring: Admin/BUH → all active PG+Leader, Leader → their
/// managed PGs (BR-405). Counts are derived from the same attendance/schedule/leave signals
/// so the dashboard agrees with the <c>/monitoring</c> list.
/// </summary>
public sealed record GetDashboardSummaryQuery(Guid ViewerId, UserRole ViewerRole)
    : IRequest<Result<DashboardSummaryDto>>;

internal sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetDashboardSummaryQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery query, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var vnOffset = TimeSpan.FromHours(7); // CR-5
        var today = DateOnly.FromDateTime(now.ToOffset(vnOffset).DateTime);
        // Midnight VN as an instant, expressed in UTC — Npgsql only writes offset-0
        // DateTimeOffset to `timestamptz`. Instant comparison semantics are unchanged.
        var dayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, vnOffset).ToUniversalTime();
        var dayEnd = dayStart.AddDays(1);

        var isLeader = query.ViewerRole == UserRole.Leader;

        // ----- Scope the team members (active PG + Leader; Leader sees only managed PGs) -----
        var membersQuery = _db.Users.AsNoTracking()
            .Where(u => u.Status == UserStatus.Active && (u.Role == UserRole.Pg || u.Role == UserRole.Leader));

        if (isLeader)
        {
            var managed = _db.UserLeaderAssignments.AsNoTracking()
                .Where(a => a.LeaderUserId == query.ViewerId && a.EffectiveTo == null)
                .Select(a => a.PgUserId);
            membersQuery = membersQuery.Where(u => managed.Contains(u.Id));
        }

        var ids = await membersQuery.Select(u => u.Id).ToListAsync(ct);
        var totalMembers = ids.Count;

        // ----- Today's signals for those users -----
        var attendance = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => ids.Contains(a.UserId) && a.CheckInAt >= dayStart && a.CheckInAt < dayEnd)
            .OrderByDescending(a => a.CheckInAt)
            .Select(a => new { a.UserId, a.Status, a.CheckOutAt })
            .ToListAsync(ct);

        var scheduledUserIds = (await _db.WorkSchedules.AsNoTracking()
            .Where(s => ids.Contains(s.UserId) && s.ScheduleDate == today && s.Status == WorkScheduleStatus.Approved)
            .Select(s => s.UserId)
            .ToListAsync(ct)).ToHashSet();

        var onLeaveUserIds = (await _db.LeaveRequests.AsNoTracking()
            .Where(r => ids.Contains(r.UserId) && r.Status == RequestStatus.Approved
                     && r.StartDate <= today && r.EndDate >= today)
            .Select(r => r.UserId)
            .ToListAsync(ct)).ToHashSet();

        var latestByUser = attendance
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        // ----- Presence counts (same precedence as M12) -----
        int online = 0, checkedOut = 0, notCheckedIn = 0, onLeave = 0;
        foreach (var id in ids)
        {
            if (onLeaveUserIds.Contains(id))
            {
                onLeave++;
            }
            else if (latestByUser.TryGetValue(id, out var att) && att.Status != AttendanceStatus.FakeGpsBlocked)
            {
                var pendingReview = att.Status is AttendanceStatus.GpsViolationPendingReview
                    or AttendanceStatus.FaceFailPendingReview;
                if (!pendingReview)
                {
                    if (att.CheckOutAt is null) online++;
                    else checkedOut++;
                }
                // pending-review records are counted in PendingReviewAttendance below, not as presence.
            }
            else if (scheduledUserIds.Contains(id))
            {
                notCheckedIn++;
            }
            // else: no schedule today → not counted as a presence KPI.
        }

        // ----- Actionable backlogs -----
        // Attendance still awaiting an Admin decision (all dates, scoped users).
        var pendingReviewAttendance = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => ids.Contains(a.UserId)
                && (a.Status == AttendanceStatus.GpsViolationPendingReview
                    || a.Status == AttendanceStatus.FaceFailPendingReview))
            .CountAsync(ct);

        // Approvals queue: Admin sees all pending; Leader/BUH see only their own queue.
        var approvalsQuery = _db.Approvals.AsNoTracking()
            .Where(a => a.Status == ApprovalStatus.Pending);
        if (query.ViewerRole != UserRole.Admin)
            approvalsQuery = approvalsQuery.Where(a => a.ApproverId == query.ViewerId);
        var pendingApprovals = await approvalsQuery.CountAsync(ct);

        // Today's anomalies (face fail / GPS violation / fake GPS) within scope.
        var anomaliesToday = attendance.Count(a =>
            a.Status is AttendanceStatus.FaceFailPendingReview
                or AttendanceStatus.GpsViolationPendingReview
                or AttendanceStatus.FakeGpsBlocked);

        return Result.Success(new DashboardSummaryDto(
            totalMembers, online, checkedOut, notCheckedIn, onLeave,
            pendingReviewAttendance, pendingApprovals, anomaliesToday, now));
    }
}
