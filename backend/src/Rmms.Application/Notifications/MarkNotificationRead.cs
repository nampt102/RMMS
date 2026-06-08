using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Application.Notifications;

/// <summary>Mark a single notification read (scoped to the owner). Idempotent.</summary>
public sealed record MarkNotificationReadCommand(Guid Id, Guid UserId) : IRequest<Result>;

/// <summary>Mark all of the caller's notifications read. Returns the number affected.</summary>
public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Result<int>>;

internal sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public MarkNotificationReadCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(MarkNotificationReadCommand command, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (n is null || n.UserId != command.UserId)
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy thông báo."));

        n.MarkRead(_clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public MarkAllNotificationsReadCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<int>> Handle(MarkAllNotificationsReadCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var unread = await _db.Notifications
            .Where(n => n.UserId == command.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread) n.MarkRead(now);

        await _db.SaveChangesAsync(ct);
        return Result.Success(unread.Count);
    }
}
