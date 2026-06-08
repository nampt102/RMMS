using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Notifications;

/// <summary>
/// Register / refresh the FCM push token for the caller's active device (M14, BR-105:
/// exactly one active device). Stored on the <c>UserDevice</c> so push targets the
/// current device only. Idempotent — re-registering the same token is a no-op.
/// </summary>
public sealed record RegisterFcmTokenCommand(Guid UserId, string Token) : IRequest<Result>;

internal sealed class RegisterFcmTokenCommandHandler : IRequestHandler<RegisterFcmTokenCommand, Result>
{
    private readonly IAppDbContext _db;
    public RegisterFcmTokenCommandHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result> Handle(RegisterFcmTokenCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, "Thiếu FCM token."));

        var device = await _db.UserDevices
            .Where(d => d.UserId == command.UserId && d.Status == DeviceStatus.Active)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (device is null)
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy thiết bị đang hoạt động."));

        device.UpdateFcmToken(command.Token);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
