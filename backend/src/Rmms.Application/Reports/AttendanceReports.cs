using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Attendance;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.Reports;

// ===== Attendance report (also serves the anomaly report via AnomaliesOnly) =====

public sealed record AttendanceReportRow(
    DateOnly Date,
    string UserName,
    string StoreName,
    DateTimeOffset CheckInAt,
    DateTimeOffset? CheckOutAt,
    string Status,
    bool IsAnomaly);

public sealed record GetAttendanceReportQuery(
    DateOnly From, DateOnly To, Guid? StoreId = null, Guid? UserId = null, bool AnomaliesOnly = false)
    : IRequest<Result<IReadOnlyList<AttendanceReportRow>>>;

internal sealed class GetAttendanceReportQueryHandler
    : IRequestHandler<GetAttendanceReportQuery, Result<IReadOnlyList<AttendanceReportRow>>>
{
    // Phase 1 ceiling for a synchronous report; async/Hangfire export is a Phase 2 item (M15 notes).
    private const int MaxRows = 20_000;
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7); // CR-5

    private readonly IAppDbContext _db;

    public GetAttendanceReportQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<AttendanceReportRow>>> Handle(GetAttendanceReportQuery query, CancellationToken ct)
    {
        if (query.To < query.From)
        {
            return Result.Failure<IReadOnlyList<AttendanceReportRow>>(
                Error.Validation(Rmms.Shared.Errors.ErrorCodes.ValidationFailed, "Khoảng ngày không hợp lệ."));
        }
        if (query.To.DayNumber - query.From.DayNumber > 366)
        {
            return Result.Failure<IReadOnlyList<AttendanceReportRow>>(
                Error.Validation(Rmms.Shared.Errors.ErrorCodes.ValidationFailed, "Tối đa 1 năm cho mỗi truy vấn."));
        }

        var records = await AttendanceReportData.QueryAsync(_db, query, MaxRows, ct);

        var (users, stores) = await AttendanceReportData.NamesAsync(_db, records, ct);
        var rows = records.Select(a => new AttendanceReportRow(
            DateOnly.FromDateTime(a.CheckInAt.ToOffset(Vn).DateTime),
            users.GetValueOrDefault(a.UserId, a.UserId.ToString()),
            a.StoreId is { } sid ? stores.GetValueOrDefault(sid, sid.ToString()) : "—",
            a.CheckInAt, a.CheckOutAt, a.Status.ToSnakeCase(), AttendanceReportData.IsAnomaly(a.Status))).ToList();

        return Result.Success<IReadOnlyList<AttendanceReportRow>>(rows);
    }
}

// ===== Attendance trend (dashboard chart) =====

public sealed record AttendanceTrendPoint(DateOnly Date, int Valid, int Late, int Anomaly);

public sealed record GetAttendanceTrendQuery(int Days = 14) : IRequest<Result<IReadOnlyList<AttendanceTrendPoint>>>;

internal sealed class GetAttendanceTrendQueryHandler
    : IRequestHandler<GetAttendanceTrendQuery, Result<IReadOnlyList<AttendanceTrendPoint>>>
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetAttendanceTrendQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<IReadOnlyList<AttendanceTrendPoint>>> Handle(GetAttendanceTrendQuery query, CancellationToken ct)
    {
        var days = Math.Clamp(query.Days, 1, 90);
        var today = DateOnly.FromDateTime(_clock.UtcNow.ToOffset(Vn).DateTime);
        var from = today.AddDays(-(days - 1));
        var fromUtc = new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, Vn).ToUniversalTime();

        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.CheckInAt >= fromUtc)
            .Select(a => new { a.CheckInAt, a.Status })
            .ToListAsync(ct);

        var byDay = records
            .GroupBy(a => DateOnly.FromDateTime(a.CheckInAt.ToOffset(Vn).DateTime))
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<AttendanceTrendPoint>(days);
        for (var i = 0; i < days; i++)
        {
            var d = from.AddDays(i);
            var bucket = byDay.GetValueOrDefault(d);
            if (bucket is null)
            {
                points.Add(new AttendanceTrendPoint(d, 0, 0, 0));
                continue;
            }
            var valid = bucket.Count(a => a.Status is AttendanceStatus.Valid or AttendanceStatus.AdminApproved);
            var late = bucket.Count(a => a.Status == AttendanceStatus.Late);
            var anomaly = bucket.Count(a => AttendanceReportData.IsAnomaly(a.Status));
            points.Add(new AttendanceTrendPoint(d, valid, late, anomaly));
        }

        return Result.Success<IReadOnlyList<AttendanceTrendPoint>>(points);
    }
}

// ===== Export (Excel) =====

public sealed record ExportAttendanceReportCommand(
    DateOnly From, DateOnly To, Guid? StoreId = null, Guid? UserId = null, bool AnomaliesOnly = false)
    : IRequest<Result<ReportFile>>;

internal sealed class ExportAttendanceReportCommandHandler : IRequestHandler<ExportAttendanceReportCommand, Result<ReportFile>>
{
    private readonly IMediator _mediator;
    private readonly IReportExporter _exporter;

    public ExportAttendanceReportCommandHandler(IMediator mediator, IReportExporter exporter)
    {
        _mediator = mediator;
        _exporter = exporter;
    }

    public async ValueTask<Result<ReportFile>> Handle(ExportAttendanceReportCommand command, CancellationToken ct)
    {
        var report = await _mediator.Send(
            new GetAttendanceReportQuery(command.From, command.To, command.StoreId, command.UserId, command.AnomaliesOnly), ct);
        if (report.IsFailure) return Result.Failure<ReportFile>(report.Error);

        var inv = CultureInfo.InvariantCulture;
        var vn = TimeSpan.FromHours(7);
        var headers = new[] { "Date", "User", "Store", "Check-in", "Check-out", "Status", "Anomaly" };
        var rows = report.Value.Select(r => (IReadOnlyList<string>)new[]
        {
            r.Date.ToString("yyyy-MM-dd", inv),
            r.UserName,
            r.StoreName,
            r.CheckInAt.ToOffset(vn).ToString("yyyy-MM-dd HH:mm", inv),
            r.CheckOutAt is { } o ? o.ToOffset(vn).ToString("yyyy-MM-dd HH:mm", inv) : "",
            r.Status,
            r.IsAnomaly ? "yes" : "",
        }).ToList();

        var title = command.AnomaliesOnly ? "Anomalies" : "Attendance";
        var sheet = new ReportSheet(title, headers, rows);
        var fileName = $"{title}_{command.From.ToString("yyyyMMdd", inv)}_{command.To.ToString("yyyyMMdd", inv)}";
        return Result.Success(_exporter.ToXlsx(sheet, fileName));
    }
}

// ----- Shared query/name/anomaly helpers -----

internal static class AttendanceReportData
{
    public static bool IsAnomaly(AttendanceStatus s) =>
        s is AttendanceStatus.GpsViolationPendingReview
            or AttendanceStatus.FaceFailPendingReview
            or AttendanceStatus.FakeGpsBlocked;

    public sealed record Rec(Guid UserId, Guid? StoreId, DateTimeOffset CheckInAt, DateTimeOffset? CheckOutAt, AttendanceStatus Status);

    public static async Task<List<Rec>> QueryAsync(IAppDbContext db, GetAttendanceReportQuery query, int maxRows, CancellationToken ct)
    {
        var fromUtc = AttendanceTime.ToUtc(query.From, TimeOnly.MinValue);
        var toUtc = AttendanceTime.ToUtc(query.To.AddDays(1), TimeOnly.MinValue);

        var q = db.AttendanceRecords.AsNoTracking()
            .Where(a => a.CheckInAt >= fromUtc && a.CheckInAt < toUtc);
        if (query.StoreId is { } sid) q = q.Where(a => a.StoreId == sid);
        if (query.UserId is { } uid) q = q.Where(a => a.UserId == uid);
        if (query.AnomaliesOnly)
        {
            q = q.Where(a => a.Status == AttendanceStatus.GpsViolationPendingReview
                || a.Status == AttendanceStatus.FaceFailPendingReview
                || a.Status == AttendanceStatus.FakeGpsBlocked);
        }

        return await q.OrderByDescending(a => a.CheckInAt)
            .Take(maxRows)
            .Select(a => new Rec(a.UserId, a.StoreId, a.CheckInAt, a.CheckOutAt, a.Status))
            .ToListAsync(ct);
    }

    public static async Task<(Dictionary<Guid, string> users, Dictionary<Guid, string> stores)> NamesAsync(
        IAppDbContext db, IReadOnlyList<Rec> records, CancellationToken ct)
    {
        var userIds = records.Select(r => r.UserId).Distinct().ToList();
        var storeIds = records.Where(r => r.StoreId is not null).Select(r => r.StoreId!.Value).Distinct().ToList();

        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var stores = await db.Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        return (users, stores);
    }
}
