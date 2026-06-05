using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Organization.Stores;

/// <summary>Paginated store list with optional area / status / search (code+name) filters.</summary>
public sealed record GetStoresQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? AreaId = null,
    string? Status = null,
    string? Search = null) : IRequest<Result<PaginatedResponse<StoreDto>>>;

internal sealed class GetStoresQueryHandler : IRequestHandler<GetStoresQuery, Result<PaginatedResponse<StoreDto>>>
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;

    public GetStoresQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<StoreDto>>> Handle(GetStoresQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.Stores.AsNoTracking().AsQueryable();

        if (query.AreaId is { } areaId)
        {
            q = q.Where(s => s.AreaId == areaId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<StoreStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var total = await q.CountAsync(ct);

        // LEFT JOIN areas so stores without an area are kept.
        var items = await q
            .OrderBy(s => s.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GroupJoin(_db.Areas.AsNoTracking(), s => s.AreaId, a => (Guid?)a.Id, (s, areas) => new { s, areas })
            .SelectMany(x => x.areas.DefaultIfEmpty(), (x, a) => new { x.s, AreaName = a != null ? a.Name : null })
            .ToListAsync(ct);

        var dtos = items
            .Select(x => new StoreDto(
                x.s.Id,
                x.s.Code,
                x.s.Name,
                x.s.Address,
                x.s.Latitude,
                x.s.Longitude,
                x.s.AreaId,
                x.AreaName,
                x.s.Status.ToSnakeCase(),
                x.s.CreatedAt,
                x.s.UpdatedAt))
            .ToList();

        return new PaginatedResponse<StoreDto>(dtos, PaginationMeta.Build(page, pageSize, total));
    }
}
