using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.TeamMonitoring;

/// <summary>Today's work status for one team member (M12, BR-601/602).</summary>
public sealed record TeamMemberStatusDto(
    Guid UserId,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset? CheckInAt,
    string? StoreName);

/// <summary>Counts by status + the per-member list + the snapshot time.</summary>
public sealed record TeamTodayDto(
    IReadOnlyList<TeamMemberStatusDto> Members,
    IReadOnlyDictionary<string, int> Summary,
    DateTimeOffset AsOf);

/// <summary>
/// Team monitoring snapshot for today (M12, AC-26/27). Scope: Admin/BUH → all PG+Leader,
/// Leader → their managed PGs (BR-405). Status computed from attendance + schedule + leave.
/// </summary>
public sealed record GetTeamTodayQuery(Guid ViewerId, UserRole ViewerRole)
    : IRequest<Result<TeamTodayDto>>;

internal static class TeamStatus
{
    public const string Working = "working";
    public const string CheckedOut = "checked_out";
    public const string NotCheckedIn = "not_checked_in";
    public const string OnLeave = "on_leave";
    public const string NoScheduleToday = "no_schedule_today";
    public const string PendingReview = "pending_review";
}

internal sealed class GetTeamTodayQueryHandler : IRequestHandler<GetTeamTodayQuery, Result<TeamTodayDto>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetTeamTodayQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<TeamTodayDto>> Handle(GetTeamTodayQuery query, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var vnOffset = TimeSpan.FromHours(7); // CR-5
        var today = DateOnly.FromDateTime((now.ToOffset(vnOffset)).DateTime);
        // Midnight VN as an instant, expressed in UTC — Npgsql only writes offset-0
        // DateTimeOffset to `timestamptz`. Instant comparison semantics are unchanged.
        var dayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, vnOffset).ToUniversalTime();
        var dayEnd = dayStart.AddDays(1);

        // ----- Scope the team members -----
        var membersQuery = _db.Users.AsNoTracking()
            .Where(u => u.Status == UserStatus.Active && (u.Role == UserRole.Pg || u.Role == UserRole.Leader));

        if (query.ViewerRole == UserRole.Leader)
        {
            var managed = _db.UserLeaderAssignments.AsNoTracking()
                .Where(a => a.LeaderUserId == query.ViewerId && a.EffectiveTo == null)
                .Select(a => a.PgUserId);
            membersQuery = membersQuery.Where(u => managed.Contains(u.Id));
        }

        var members = await membersQuery
            .Select(u => new { u.Id, u.FullName, u.Role })
            .ToListAsync(ct);
        var ids = members.Select(m => m.Id).ToList();

        // ----- Today's signals for those users -----
        var attendance = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => ids.Contains(a.UserId) && a.CheckInAt >= dayStart && a.CheckInAt < dayEnd)
            .OrderByDescending(a => a.CheckInAt)
            .Select(a => new { a.UserId, a.Status, a.CheckOutAt, a.CheckInAt, a.StoreId })
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

        var storeNames = await _db.Stores.AsNoTracking()
            .Where(s => attendance.Select(a => a.StoreId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        // Most-recent attendance per user (list is already newest-first).
        var latestByUser = attendance
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        // ----- Compute status per member -----
        var list = new List<TeamMemberStatusDto>(members.Count);
        foreach (var m in members)
        {
            string status;
            DateTimeOffset? checkInAt = null;
            string? storeName = null;

            if (onLeaveUserIds.Contains(m.Id))
            {
                status = TeamStatus.OnLeave;
            }
            else if (latestByUser.TryGetValue(m.Id, out var att) && att.Status != AttendanceStatus.FakeGpsBlocked)
            {
                checkInAt = att.CheckInAt;
                storeName = storeNames.GetValueOrDefault(att.StoreId);
                status = att.Status is AttendanceStatus.GpsViolationPendingReview or AttendanceStatus.FaceFailPendingReview
                    ? TeamStatus.PendingReview
                    : att.CheckOutAt is null
                        ? TeamStatus.Working
                        : TeamStatus.CheckedOut;
            }
            else if (scheduledUserIds.Contains(m.Id))
            {
                status = TeamStatus.NotCheckedIn;
            }
            else
            {
                status = TeamStatus.NoScheduleToday;
            }

            list.Add(new TeamMemberStatusDto(m.Id, m.FullName, m.Role.ToString().ToLowerInvariant(), status, checkInAt, storeName));
        }

        var ordered = list
            .OrderBy(x => StatusRank(x.Status))
            .ThenBy(x => x.FullName)
            .ToList();
        var summary = list.GroupBy(x => x.Status).ToDictionary(g => g.Key, g => g.Count());

        return Result.Success(new TeamTodayDto(ordered, summary, now));
    }

    // Surface the actionable statuses first.
    private static int StatusRank(string s) => s switch
    {
        TeamStatus.PendingReview => 0,
        TeamStatus.NotCheckedIn => 1,
        TeamStatus.Working => 2,
        TeamStatus.CheckedOut => 3,
        TeamStatus.OnLeave => 4,
        _ => 5,
    };
}
