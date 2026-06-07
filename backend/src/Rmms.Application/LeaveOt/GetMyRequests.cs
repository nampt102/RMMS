using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;

namespace Rmms.Application.LeaveOt;

public sealed record GetMyLeaveRequestsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<LeaveRequestDto>>>;

public sealed record GetMyOtRequestsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<OtRequestDto>>>;

internal sealed class GetMyLeaveRequestsQueryHandler
    : IRequestHandler<GetMyLeaveRequestsQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly IAppDbContext _db;
    public GetMyLeaveRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<LeaveRequestDto>>> Handle(GetMyLeaveRequestsQuery query, CancellationToken ct)
    {
        var rows = await _db.LeaveRequests.AsNoTracking()
            .Where(r => r.UserId == query.UserId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);
        IReadOnlyList<LeaveRequestDto> dtos = rows.Select(r => LeaveOtMapper.ToDto(r)).ToList();
        return Result.Success(dtos);
    }
}

internal sealed class GetMyOtRequestsQueryHandler
    : IRequestHandler<GetMyOtRequestsQuery, Result<IReadOnlyList<OtRequestDto>>>
{
    private readonly IAppDbContext _db;
    public GetMyOtRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<OtRequestDto>>> Handle(GetMyOtRequestsQuery query, CancellationToken ct)
    {
        var rows = await _db.OtRequests.AsNoTracking()
            .Where(r => r.UserId == query.UserId)
            .OrderByDescending(r => r.OtDate)
            .ToListAsync(ct);
        IReadOnlyList<OtRequestDto> dtos = rows.Select(r => LeaveOtMapper.ToDto(r)).ToList();
        return Result.Success(dtos);
    }
}
