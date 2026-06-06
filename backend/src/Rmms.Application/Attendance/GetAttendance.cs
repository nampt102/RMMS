using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Attendance;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Attendance;

// ===== Caller's attendance history (paginated) =====

public sealed record GetHistoryQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page = 1, int PageSize = 20)
    : IRequest<Result<PaginatedResponse<AttendanceDto>>>;

internal sealed class GetHistoryQueryHandler : IRequestHandler<GetHistoryQuery, Result<PaginatedResponse<AttendanceDto>>>
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    private readonly IAttendancePhotoStorage _photos;

    public GetHistoryQueryHandler(IAppDbContext db, IAttendancePhotoStorage photos)
    {
        _db = db;
        _photos = photos;
    }

    public async ValueTask<Result<PaginatedResponse<AttendanceDto>>> Handle(GetHistoryQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.AttendanceRecords.AsNoTracking().Where(a => a.UserId == query.UserId);
        if (query.From is { } from) q = q.Where(a => a.CheckInAt >= AttendanceTime.ToUtc(from, TimeOnly.MinValue));
        if (query.To is { } to) q = q.Where(a => a.CheckInAt < AttendanceTime.ToUtc(to.AddDays(1), TimeOnly.MinValue));

        var total = await q.CountAsync(ct);
        var records = await q
            .OrderByDescending(a => a.CheckInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await AttendanceQueries.MapWithStoresAsync(_db, records, ct);
        items = await AttendanceQueries.PresignAllAsync(_photos, items, ct);
        return new PaginatedResponse<AttendanceDto>(items, PaginationMeta.Build(page, pageSize, total));
    }
}

// ===== Today's expected shifts + their attendance status =====

public sealed record GetTodayQuery(Guid UserId) : IRequest<Result<IReadOnlyList<TodayShiftDto>>>;

internal sealed class GetTodayQueryHandler : IRequestHandler<GetTodayQuery, Result<IReadOnlyList<TodayShiftDto>>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetTodayQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<IReadOnlyList<TodayShiftDto>>> Handle(GetTodayQuery query, CancellationToken ct)
    {
        var vnToday = AttendanceTime.VnToday(_clock.UtcNow);
        var shifts = await AttendanceQueries.TodayShiftsAsync(_db, query.UserId, vnToday, ct);
        return Result.Success(shifts);
    }
}

// ===== Check-in screen bootstrap: assigned stores + today's shifts + thresholds =====

public sealed record GetCheckInInfoQuery(Guid UserId) : IRequest<Result<CheckInInfoDto>>;

internal sealed class GetCheckInInfoQueryHandler : IRequestHandler<GetCheckInInfoQuery, Result<CheckInInfoDto>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetCheckInInfoQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<CheckInInfoDto>> Handle(GetCheckInInfoQuery query, CancellationToken ct)
    {
        var vnToday = AttendanceTime.VnToday(_clock.UtcNow);

        var assignedStoreIds = await _db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.UserId == query.UserId && a.EffectiveFrom <= vnToday
                        && (a.EffectiveTo == null || a.EffectiveTo >= vnToday))
            .Select(a => a.StoreId)
            .Distinct()
            .ToListAsync(ct);

        var stores = await _db.Stores.AsNoTracking()
            .Where(s => assignedStoreIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(s => new AssignedStoreDto(s.Id, s.Code, s.Name, s.Address, s.Latitude, s.Longitude))
            .ToListAsync(ct);

        var todayShifts = await AttendanceQueries.TodayShiftsAsync(_db, query.UserId, vnToday, ct);

        return Result.Success(new CheckInInfoDto(
            AttendanceRecord.GeofenceRadiusMeters,
            AttendanceTime.EarlyCheckInMinutes,
            AttendanceTime.LateThresholdMinutes,
            stores,
            todayShifts));
    }
}

// ===== Shared query helpers =====

internal static class AttendanceQueries
{
    /// <summary>Replace stored photo keys on a DTO with short-lived presigned preview URLs.</summary>
    public static async Task<AttendanceDto> PresignAsync(
        IAttendancePhotoStorage storage, AttendanceDto dto, CancellationToken ct) =>
        dto with
        {
            CheckInSelfieUrl = await storage.GetUrlAsync(dto.CheckInSelfieUrl, ct),
            CheckInStorePhotoUrl = await storage.GetUrlAsync(dto.CheckInStorePhotoUrl, ct),
            CheckOutSelfieUrl = await storage.GetUrlAsync(dto.CheckOutSelfieUrl, ct),
            CheckOutStorePhotoUrl = await storage.GetUrlAsync(dto.CheckOutStorePhotoUrl, ct),
        };

    /// <summary>Presign a whole page of DTOs.</summary>
    public static async Task<IReadOnlyList<AttendanceDto>> PresignAllAsync(
        IAttendancePhotoStorage storage, IReadOnlyList<AttendanceDto> dtos, CancellationToken ct)
    {
        var list = new List<AttendanceDto>(dtos.Count);
        foreach (var d in dtos) list.Add(await PresignAsync(storage, d, ct));
        return list;
    }

    /// <summary>Resolve a store-label lookup for a set of records, then map them to DTOs.</summary>
    public static async Task<IReadOnlyList<AttendanceDto>> MapWithStoresAsync(
        IAppDbContext db, IReadOnlyList<AttendanceRecord> records, CancellationToken ct)
    {
        if (records.Count == 0) return Array.Empty<AttendanceDto>();

        var storeIds = records.Select(r => r.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Code, s.Name })
            .ToListAsync(ct);
        var lookup = stores.ToDictionary(s => s.Id, s => (s.Code, s.Name));

        return records
            .Select(r =>
            {
                var label = lookup.TryGetValue(r.StoreId, out var v) ? v : (Code: string.Empty, Name: string.Empty);
                return AttendanceMapper.ToDto(r, label.Code, label.Name);
            })
            .ToList();
    }

    /// <summary>Today's approved shifts for a user, joined to their current attendance (if any).</summary>
    public static async Task<IReadOnlyList<TodayShiftDto>> TodayShiftsAsync(
        IAppDbContext db, Guid userId, DateOnly vnToday, CancellationToken ct)
    {
        var schedules = await db.WorkSchedules.AsNoTracking()
            .Where(s => s.UserId == userId && s.ScheduleDate == vnToday && s.Status == WorkScheduleStatus.Approved)
            .ToListAsync(ct);

        var shifts = schedules.SelectMany(s => s.Shifts).OrderBy(sh => sh.StartTime).ToList();
        if (shifts.Count == 0) return Array.Empty<TodayShiftDto>();

        var storeIds = shifts.Select(sh => sh.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Code, s.Name, s.Latitude, s.Longitude })
            .ToListAsync(ct);
        var storeLookup = stores.ToDictionary(s => s.Id);

        var shiftIds = shifts.Select(sh => sh.Id).ToList();
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(a => shiftIds.Contains(a.WorkScheduleShiftId))
            .Select(a => new { a.Id, a.WorkScheduleShiftId, a.Status, a.CheckInAt, a.CheckOutAt })
            .ToListAsync(ct);
        var recordLookup = records
            .GroupBy(r => r.WorkScheduleShiftId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CheckInAt).First());

        return shifts.Select(sh =>
        {
            storeLookup.TryGetValue(sh.StoreId, out var store);
            recordLookup.TryGetValue(sh.Id, out var rec);
            return new TodayShiftDto(
                sh.Id,
                sh.StoreId,
                store?.Code ?? string.Empty,
                store?.Name ?? string.Empty,
                store?.Latitude ?? 0m,
                store?.Longitude ?? 0m,
                sh.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                sh.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                rec?.Id,
                rec is null ? null : rec.Status.ToSnakeCase(),
                rec?.CheckInAt,
                rec?.CheckOutAt);
        }).ToList();
    }
}
