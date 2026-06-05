using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;

namespace Rmms.Application.Organization.Me;

/// <summary>The current PG's active managing Leader, for the mobile app (`GET /users/me/leader`).</summary>
public sealed record MyLeaderDto(Guid LeaderUserId, string FullName, string Email, string? Phone);

/// <summary>
/// Active Leader for the calling PG, or null if none assigned. Identity from JWT.
/// (Returns null for non-PG users — they are not the PG side of a leader assignment.)
/// </summary>
public sealed record GetMyLeaderQuery(Guid UserId) : IRequest<Result<MyLeaderDto?>>;

internal sealed class GetMyLeaderQueryHandler : IRequestHandler<GetMyLeaderQuery, Result<MyLeaderDto?>>
{
    private readonly IAppDbContext _db;

    public GetMyLeaderQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<MyLeaderDto?>> Handle(GetMyLeaderQuery query, CancellationToken ct)
    {
        var leader = await _db.UserLeaderAssignments.AsNoTracking()
            .Where(a => a.PgUserId == query.UserId && a.EffectiveTo == null)
            .Join(_db.Users.AsNoTracking(), a => a.LeaderUserId, u => u.Id,
                (a, u) => new MyLeaderDto(u.Id, u.FullName, u.Email, u.Phone))
            .FirstOrDefaultAsync(ct);

        return Result.Success(leader);
    }
}
