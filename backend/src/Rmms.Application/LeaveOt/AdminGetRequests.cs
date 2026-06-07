using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Pagination;

namespace Rmms.Application.LeaveOt;

/// <summary>Admin: all leave requests (optional status filter), paginated, with requester name.</summary>
public sealed record GetAllLeaveRequestsQuery(string? Status, int Page, int PageSize)
    : IRequest<Result<PaginatedResponse<LeaveRequestDto>>>;

/// <summary>Admin: all OT requests (optional status filter), paginated, with requester name.</summary>
public sealed record GetAllOtRequestsQuery(string? Status, int Page, int PageSize)
    : IRequest<Result<PaginatedResponse<OtRequestDto>>>;

internal sealed class GetAllLeaveRequestsQueryHandler
    : IRequestHandler<GetAllLeaveRequestsQuery, Result<PaginatedResponse<LeaveRequestDto>>>
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    public GetAllLeaveRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<LeaveRequestDto>>> Handle(GetAllLeaveRequestsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.LeaveRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RequestStatus>(query.Status, ignoreCase: true, out var s))
            q = q.Where(r => r.Status == s);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_db.Users.AsNoTracking(), r => r.UserId, u => u.Id, (r, u) => new { r, u.FullName })
            .ToListAsync(ct);

        var items = rows.Select(x => LeaveOtMapper.ToDto(x.r, x.FullName)).ToList();
        return new PaginatedResponse<LeaveRequestDto>(items, PaginationMeta.Build(page, pageSize, total));
    }
}

internal sealed class GetAllOtRequestsQueryHandler
    : IRequestHandler<GetAllOtRequestsQuery, Result<PaginatedResponse<OtRequestDto>>>
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    public GetAllOtRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<OtRequestDto>>> Handle(GetAllOtRequestsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.OtRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RequestStatus>(query.Status, ignoreCase: true, out var s))
            q = q.Where(r => r.Status == s);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_db.Users.AsNoTracking(), r => r.UserId, u => u.Id, (r, u) => new { r, u.FullName })
            .ToListAsync(ct);

        var items = rows.Select(x => LeaveOtMapper.ToDto(x.r, x.FullName)).ToList();
        return new PaginatedResponse<OtRequestDto>(items, PaginationMeta.Build(page, pageSize, total));
    }
}
