using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Views;

// ===== DTOs =====

/// <summary>A person node in the org views (role/status already lowercased for the web).</summary>
public sealed record OrgPersonDto(Guid Id, string FullName, string Email, string Role, string Status);

/// <summary>A Leader with the PGs reporting to them (active <c>user_leader_assignments</c>).</summary>
public sealed record OrgLeaderNodeDto(
    Guid Id, string FullName, string Email, string Status, IReadOnlyList<OrgPersonDto> Pgs);

/// <summary>
/// Rank hierarchy: BUH (domain-wide management tier) → Leaders → their PGs, plus PGs with no
/// active Leader. The web renders BUHs as the top tier above all Leaders (BUH is not partitioned
/// per Leader — see BR-406 / PRD), so Leaders hang under the BUH tier as a whole.
/// </summary>
public sealed record OrgHierarchyDto(
    IReadOnlyList<OrgPersonDto> Buhs,
    IReadOnlyList<OrgLeaderNodeDto> Leaders,
    IReadOnlyList<OrgPersonDto> UnassignedPgs);

/// <summary>An employee assigned to a store (active <c>user_store_assignments</c>).</summary>
public sealed record OrgStoreEmployeeDto(Guid Id, string FullName, string Role, string Status);

/// <summary>A store with the employees assigned to it.</summary>
public sealed record OrgStoreNodeDto(
    Guid Id, string Code, string Name, string Status, IReadOnlyList<OrgStoreEmployeeDto> Employees);

/// <summary>
/// Area view: a flat list of areas (each carrying its stores + assigned employees) with
/// <c>ParentAreaId</c> so the web builds the parent→child tree, plus stores with no area.
/// </summary>
public sealed record OrgAreaTreeDto(
    IReadOnlyList<OrgAreaNodeDto> Areas,
    IReadOnlyList<OrgStoreNodeDto> UnassignedStores);

public sealed record OrgAreaNodeDto(
    Guid Id, string Code, string Name, Guid? ParentAreaId, IReadOnlyList<OrgStoreNodeDto> Stores);

// ===== Query: rank hierarchy (BUH → Leader → PG) =====

public sealed record GetOrgHierarchyQuery() : IRequest<Result<OrgHierarchyDto>>;

internal sealed class GetOrgHierarchyQueryHandler : IRequestHandler<GetOrgHierarchyQuery, Result<OrgHierarchyDto>>
{
    private readonly IAppDbContext _db;

    public GetOrgHierarchyQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<OrgHierarchyDto>> Handle(GetOrgHierarchyQuery query, CancellationToken ct)
    {
        var people = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Pg || u.Role == UserRole.Leader || u.Role == UserRole.Buh)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.Status })
            .ToListAsync(ct);

        // PG → Leader, active assignments only (a PG has at most one).
        var leaderByPg = await _db.UserLeaderAssignments.AsNoTracking()
            .Where(a => a.EffectiveTo == null)
            .Select(a => new { a.PgUserId, a.LeaderUserId })
            .ToDictionaryAsync(a => a.PgUserId, a => a.LeaderUserId, ct);

        var pgs = people.Where(p => p.Role == UserRole.Pg).OrderBy(p => p.FullName).ToList();
        var pgsByLeader = pgs
            .Where(p => leaderByPg.ContainsKey(p.Id))
            .GroupBy(p => leaderByPg[p.Id])
            .ToDictionary(g => g.Key, g => g.ToList());

        var buhs = people.Where(p => p.Role == UserRole.Buh).OrderBy(p => p.FullName)
            .Select(p => new OrgPersonDto(p.Id, p.FullName, p.Email, p.Role.ToSnakeCase(), p.Status.ToSnakeCase()))
            .ToList();

        var leaders = people.Where(p => p.Role == UserRole.Leader).OrderBy(p => p.FullName)
            .Select(l => new OrgLeaderNodeDto(
                l.Id, l.FullName, l.Email, l.Status.ToSnakeCase(),
                (pgsByLeader.TryGetValue(l.Id, out var members) ? members : new())
                    .Select(p => new OrgPersonDto(p.Id, p.FullName, p.Email, p.Role.ToSnakeCase(), p.Status.ToSnakeCase()))
                    .ToList()))
            .ToList();

        var unassignedPgs = pgs.Where(p => !leaderByPg.ContainsKey(p.Id))
            .Select(p => new OrgPersonDto(p.Id, p.FullName, p.Email, p.Role.ToSnakeCase(), p.Status.ToSnakeCase()))
            .ToList();

        return Result.Success(new OrgHierarchyDto(buhs, leaders, unassignedPgs));
    }
}

// ===== Query: area tree (Area → Store → assigned employees) =====

public sealed record GetOrgAreaTreeQuery() : IRequest<Result<OrgAreaTreeDto>>;

internal sealed class GetOrgAreaTreeQueryHandler : IRequestHandler<GetOrgAreaTreeQuery, Result<OrgAreaTreeDto>>
{
    private readonly IAppDbContext _db;

    public GetOrgAreaTreeQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<OrgAreaTreeDto>> Handle(GetOrgAreaTreeQuery query, CancellationToken ct)
    {
        var areas = await _db.Areas.AsNoTracking()
            .Select(a => new { a.Id, a.Code, a.Name, a.ParentAreaId })
            .ToListAsync(ct);

        var stores = await _db.Stores.AsNoTracking()
            .Select(s => new { s.Id, s.Code, s.Name, s.AreaId, s.Status })
            .ToListAsync(ct);

        // Active store assignments joined to the assigned user (PG/Leader).
        var empRows = await _db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.EffectiveTo == null)
            .Join(_db.Users.AsNoTracking(), a => a.UserId, u => u.Id,
                (a, u) => new { a.StoreId, u.Id, u.FullName, u.Role, u.Status })
            .ToListAsync(ct);
        var empByStore = empRows
            .GroupBy(e => e.StoreId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OrgStoreEmployeeDto>)g
                    .OrderBy(e => e.Role).ThenBy(e => e.FullName)
                    .Select(e => new OrgStoreEmployeeDto(e.Id, e.FullName, e.Role.ToSnakeCase(), e.Status.ToSnakeCase()))
                    .ToList());

        OrgStoreNodeDto ToStoreNode(Guid id, string code, string name, StoreStatus status) =>
            new(id, code, name, status.ToSnakeCase(),
                empByStore.TryGetValue(id, out var emps) ? emps : Array.Empty<OrgStoreEmployeeDto>());

        var storesByArea = stores
            .Where(s => s.AreaId != null)
            .GroupBy(s => s.AreaId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Code).ToList());

        var areaNodes = areas
            .OrderBy(a => a.Code)
            .Select(a => new OrgAreaNodeDto(
                a.Id, a.Code, a.Name, a.ParentAreaId,
                (storesByArea.TryGetValue(a.Id, out var areaStores) ? areaStores : new())
                    .Select(s => ToStoreNode(s.Id, s.Code, s.Name, s.Status))
                    .ToList()))
            .ToList();

        var unassignedStores = stores
            .Where(s => s.AreaId == null)
            .OrderBy(s => s.Code)
            .Select(s => ToStoreNode(s.Id, s.Code, s.Name, s.Status))
            .ToList();

        return Result.Success(new OrgAreaTreeDto(areaNodes, unassignedStores));
    }
}
