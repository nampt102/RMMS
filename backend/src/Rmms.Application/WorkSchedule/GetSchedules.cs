using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Application.Scheduling;

// ===== Current user's schedule in a date range =====

public sealed record GetMyScheduleQuery(Guid UserId, DateOnly From, DateOnly To)
    : IRequest<Result<IReadOnlyList<WorkScheduleDto>>>;

internal sealed class GetMyScheduleQueryHandler
    : IRequestHandler<GetMyScheduleQuery, Result<IReadOnlyList<WorkScheduleDto>>>
{
    private readonly IAppDbContext _db;

    public GetMyScheduleQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<WorkScheduleDto>>> Handle(GetMyScheduleQuery query, CancellationToken ct)
    {
        var result = await ScheduleLoader.LoadAsync(_db, query.UserId, query.From, query.To, ct);
        return Result.Success(result);
    }
}

// ===== Leader/Admin views a user's schedule =====

public sealed record GetUserScheduleQuery(Guid CallerUserId, bool CallerIsAdmin, Guid TargetUserId, DateOnly From, DateOnly To)
    : IRequest<Result<IReadOnlyList<WorkScheduleDto>>>;

internal sealed class GetUserScheduleQueryHandler
    : IRequestHandler<GetUserScheduleQuery, Result<IReadOnlyList<WorkScheduleDto>>>
{
    private readonly IAppDbContext _db;

    public GetUserScheduleQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<WorkScheduleDto>>> Handle(GetUserScheduleQuery query, CancellationToken ct)
    {
        // Leader-scoping (BR-405): a Leader may only view schedules of PGs they actively manage.
        if (!query.CallerIsAdmin)
        {
            var manages = await _db.UserLeaderAssignments.AsNoTracking().AnyAsync(
                a => a.LeaderUserId == query.CallerUserId && a.PgUserId == query.TargetUserId && a.EffectiveTo == null, ct);
            if (!manages)
            {
                return Result.Failure<IReadOnlyList<WorkScheduleDto>>(
                    Error.Forbidden(ErrorCodes.NotApprover, "Bạn không quản lý người dùng này."));
            }
        }

        var result = await ScheduleLoader.LoadAsync(_db, query.TargetUserId, query.From, query.To, ct);
        return Result.Success(result);
    }
}

// ===== Shared loader =====

internal static class ScheduleLoader
{
    public static async Task<IReadOnlyList<WorkScheduleDto>> LoadAsync(
        IAppDbContext db, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        // Shifts are an owned collection → loaded automatically with each schedule.
        var schedules = await db.WorkSchedules.AsNoTracking()
            .Where(s => s.UserId == userId && s.ScheduleDate >= from && s.ScheduleDate <= to)
            .OrderBy(s => s.ScheduleDate).ThenBy(s => s.Version)
            .ToListAsync(ct);

        var storeIds = schedules.SelectMany(s => s.Shifts).Select(sh => sh.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Code, s.Name })
            .ToListAsync(ct);

        IReadOnlyDictionary<Guid, (string Code, string Name)> lookup =
            stores.ToDictionary(s => s.Id, s => (s.Code, s.Name));

        return schedules.Select(s => WorkScheduleMapper.ToDto(s, lookup)).ToList();
    }
}
