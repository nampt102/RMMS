using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.VisitPlans;

// ===== Leader's own plans (mobile) =====

public sealed record GetMyVisitPlansQuery(Guid LeaderUserId) : IRequest<Result<IReadOnlyList<VisitPlanDto>>>;

internal sealed class GetMyVisitPlansQueryHandler
    : IRequestHandler<GetMyVisitPlansQuery, Result<IReadOnlyList<VisitPlanDto>>>
{
    private readonly IAppDbContext _db;

    public GetMyVisitPlansQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<VisitPlanDto>>> Handle(GetMyVisitPlansQuery query, CancellationToken ct)
    {
        var plans = await _db.VisitPlans.AsNoTracking()
            .Where(p => p.LeaderUserId == query.LeaderUserId)
            .OrderByDescending(p => p.VisitDate).ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var lookups = await VisitPlanNames.BuildAsync(_db, plans, ct);
        var items = plans.Select(p => VisitPlanMapper.ToDto(p, lookups: lookups)).ToList();
        return Result.Success<IReadOnlyList<VisitPlanDto>>(items);
    }
}

// ===== Single plan detail (Leader owner or Admin) =====

public sealed record GetVisitPlanQuery(Guid PlanId, Guid ViewerId, UserRole ViewerRole)
    : IRequest<Result<VisitPlanDto>>;

internal sealed class GetVisitPlanQueryHandler : IRequestHandler<GetVisitPlanQuery, Result<VisitPlanDto>>
{
    private readonly IAppDbContext _db;

    public GetVisitPlanQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<VisitPlanDto>> Handle(GetVisitPlanQuery query, CancellationToken ct)
    {
        var plan = await _db.VisitPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == query.PlanId, ct);
        if (plan is null)
        {
            return Result.Failure<VisitPlanDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy kế hoạch viếng thăm."));
        }

        // Admin sees any; a Leader sees only their own.
        if (query.ViewerRole != UserRole.Admin && plan.LeaderUserId != query.ViewerId)
        {
            return Result.Failure<VisitPlanDto>(Error.Forbidden(ErrorCodes.PermissionDenied, "Không có quyền xem kế hoạch này."));
        }

        var leaderName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == plan.LeaderUserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var lookups = await VisitPlanNames.BuildAsync(_db, new[] { plan }, ct);

        return Result.Success(VisitPlanMapper.ToDto(plan, leaderName, lookups));
    }
}

// ===== Admin view of all plans =====

public sealed record AdminGetVisitPlansQuery(string? Status, Guid? LeaderUserId)
    : IRequest<Result<IReadOnlyList<VisitPlanDto>>>;

internal sealed class AdminGetVisitPlansQueryHandler
    : IRequestHandler<AdminGetVisitPlansQuery, Result<IReadOnlyList<VisitPlanDto>>>
{
    private readonly IAppDbContext _db;

    public AdminGetVisitPlansQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<VisitPlanDto>>> Handle(AdminGetVisitPlansQuery query, CancellationToken ct)
    {
        var q = _db.VisitPlans.AsNoTracking().AsQueryable();

        if (query.LeaderUserId is { } leaderId)
        {
            q = q.Where(p => p.LeaderUserId == leaderId);
        }
        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<VisitPlanStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(p => p.Status == status);
        }

        var plans = await q
            .OrderByDescending(p => p.VisitDate).ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var leaderIds = plans.Select(p => p.LeaderUserId).Distinct().ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => leaderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var lookups = await VisitPlanNames.BuildAsync(_db, plans, ct);

        var items = plans.Select(p => VisitPlanMapper.ToDto(
            p, names.TryGetValue(p.LeaderUserId, out var n) ? n : null, lookups)).ToList();
        return Result.Success<IReadOnlyList<VisitPlanDto>>(items);
    }
}

/// <summary>Resolves store + form display names for a set of plans (one query each).</summary>
internal static class VisitPlanNames
{
    public static async Task<VisitPlanLookups> BuildAsync(
        IAppDbContext db, IReadOnlyList<Domain.VisitPlan.VisitPlan> plans, CancellationToken ct)
    {
        var storeIds = plans.SelectMany(p => p.Items).Select(i => i.StoreId).Distinct().ToList();
        var formIds = plans.SelectMany(p => p.Items).Select(i => i.FormId).Distinct().ToList();

        var stores = await db.Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var forms = await db.Forms.AsNoTracking()
            .Where(f => formIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.NameVi, ct);

        return new VisitPlanLookups(stores, forms);
    }
}
