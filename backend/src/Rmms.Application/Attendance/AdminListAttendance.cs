using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Attendance;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Attendance;

/// <summary>
/// Admin attendance list (M05 — "Attendance list with filter" + anomaly review queue).
/// Filterable by user, store, status, and check-in date range. Ordered newest-first.
/// </summary>
public sealed record AdminListAttendanceQuery(
    Guid? UserId = null,
    Guid? StoreId = null,
    string? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedResponse<AttendanceDto>>>;

internal sealed class AdminListAttendanceQueryHandler
    : IRequestHandler<AdminListAttendanceQuery, Result<PaginatedResponse<AttendanceDto>>>
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public AdminListAttendanceQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<AttendanceDto>>> Handle(AdminListAttendanceQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.AttendanceRecords.AsNoTracking().AsQueryable();

        if (query.UserId is { } userId) q = q.Where(a => a.UserId == userId);
        if (query.StoreId is { } storeId) q = q.Where(a => a.StoreId == storeId);
        if (!string.IsNullOrWhiteSpace(query.Status) && TryParseStatus(query.Status, out var status))
        {
            q = q.Where(a => a.Status == status);
        }
        if (query.From is { } from) q = q.Where(a => a.CheckInAt >= AttendanceTime.ToUtc(from, TimeOnly.MinValue));
        if (query.To is { } to) q = q.Where(a => a.CheckInAt < AttendanceTime.ToUtc(to.AddDays(1), TimeOnly.MinValue));

        var total = await q.CountAsync(ct);
        var records = await q
            .OrderByDescending(a => a.CheckInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await AttendanceQueries.MapWithStoresAsync(_db, records, ct);
        return new PaginatedResponse<AttendanceDto>(items, PaginationMeta.Build(page, pageSize, total));
    }

    private static bool TryParseStatus(string raw, out AttendanceStatus status)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "valid": status = AttendanceStatus.Valid; return true;
            case "late": status = AttendanceStatus.Late; return true;
            case "gps_violation_pending_review": status = AttendanceStatus.GpsViolationPendingReview; return true;
            case "face_fail_pending_review": status = AttendanceStatus.FaceFailPendingReview; return true;
            case "fake_gps_blocked": status = AttendanceStatus.FakeGpsBlocked; return true;
            case "admin_approved": status = AttendanceStatus.AdminApproved; return true;
            case "admin_rejected": status = AttendanceStatus.AdminRejected; return true;
            default: status = default; return false;
        }
    }
}
