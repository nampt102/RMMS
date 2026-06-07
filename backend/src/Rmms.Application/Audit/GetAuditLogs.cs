using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Audit;

/// <summary>One audit-log row projected for the Admin explorer (M16, AC-35).</summary>
public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorName,
    string Action,
    string TargetEntity,
    Guid? TargetId,
    string? IpAddress,
    string Metadata,
    DateTimeOffset CreatedAt);

/// <summary>Admin audit-log search (filters + pagination). Append-only table — read only.</summary>
public sealed record GetAuditLogsQuery(
    string? Action, string? TargetEntity, Guid? ActorUserId, DateTimeOffset? From, DateTimeOffset? To, int Page, int PageSize)
    : IRequest<Result<PaginatedResponse<AuditLogDto>>>;

internal sealed class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, Result<PaginatedResponse<AuditLogDto>>>
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public GetAuditLogsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<AuditLogDto>>> Handle(GetAuditLogsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var entity = query.TargetEntity?.Trim().ToLowerInvariant();

        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Action)) q = q.Where(a => a.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(entity)) q = q.Where(a => a.TargetEntity == entity);
        if (query.ActorUserId is { } actor) q = q.Where(a => a.ActorUserId == actor);
        if (query.From is { } from) q = q.Where(a => a.CreatedAt >= from);
        if (query.To is { } to) q = q.Where(a => a.CreatedAt <= to);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GroupJoin(_db.Users.AsNoTracking(), a => a.ActorUserId, u => u.Id, (a, us) => new { a, us })
            .SelectMany(x => x.us.DefaultIfEmpty(), (x, u) => new { x.a, u })
            .ToListAsync(ct);

        var items = rows.Select(x => new AuditLogDto(
            x.a.Id, x.a.ActorUserId, x.u != null ? x.u.FullName : null,
            x.a.Action, x.a.TargetEntity, x.a.TargetId,
            x.a.IpAddress != null ? x.a.IpAddress.ToString() : null,
            x.a.Metadata, x.a.CreatedAt)).ToList();

        return new PaginatedResponse<AuditLogDto>(items, PaginationMeta.Build(page, pageSize, total));
    }
}
