using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;

namespace Rmms.Application.Notifications;

/// <summary>Recipient's notification inbox — newest first, paginated, with unread badge count (M14).</summary>
public sealed record GetMyNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<NotificationListDto>>;

internal sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<NotificationListDto>>
{
    private readonly IAppDbContext _db;
    public GetMyNotificationsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<NotificationListDto>> Handle(GetMyNotificationsQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var baseQuery = _db.Notifications.AsNoTracking().Where(n => n.UserId == query.UserId);

        var total = await baseQuery.CountAsync(ct);
        var unread = await baseQuery.CountAsync(n => !n.IsRead, ct);

        var rows = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        IReadOnlyList<NotificationDto> items = rows.Select(NotificationMapper.ToDto).ToList();
        return Result.Success(new NotificationListDto(items, unread, page, pageSize, total));
    }
}
