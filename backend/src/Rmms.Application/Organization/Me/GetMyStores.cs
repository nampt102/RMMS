using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;

namespace Rmms.Application.Organization.Me;

/// <summary>Store assigned to the current user, for the mobile app (M03 — `GET /users/me/stores`).</summary>
public sealed record MyStoreDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    string Status);

/// <summary>
/// Active store assignments for the calling user (PG or Leader). "Active" = the assignment
/// is open-ended or not yet ended. Identity comes from the JWT (set by the controller).
/// </summary>
public sealed record GetMyStoresQuery(Guid UserId) : IRequest<Result<IReadOnlyList<MyStoreDto>>>;

internal sealed class GetMyStoresQueryHandler : IRequestHandler<GetMyStoresQuery, Result<IReadOnlyList<MyStoreDto>>>
{
    private readonly IAppDbContext _db;

    public GetMyStoresQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<MyStoreDto>>> Handle(GetMyStoresQuery query, CancellationToken ct)
    {
        var rows = await _db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.UserId == query.UserId && a.EffectiveTo == null)
            .Join(_db.Stores.AsNoTracking(), a => a.StoreId, s => s.Id, (a, s) => s)
            .OrderBy(s => s.Code)
            .Select(s => new { s.Id, s.Code, s.Name, s.Address, s.Latitude, s.Longitude, s.Status })
            .ToListAsync(ct);

        IReadOnlyList<MyStoreDto> result = rows
            .Select(s => new MyStoreDto(s.Id, s.Code, s.Name, s.Address, s.Latitude, s.Longitude, s.Status.ToSnakeCase()))
            .ToList();

        return Result.Success(result);
    }
}
