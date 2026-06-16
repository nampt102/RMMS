using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Domain.Notifications;
using Rmms.Shared.Errors;

namespace Rmms.Application.Notifications;

/// <summary>
/// Register / refresh the caller's FCM push token (M14, CR-3).
///
/// The token is stored in <c>push_tokens</c> for ANY authenticated user, decoupled from the
/// PG-only device-lock (BR-105) so Leader/BUH — who use the app but carry no active device row —
/// also receive push. The token identifies one install, so it is re-pointed to whoever last
/// registered it (shared phone / re-login). When the caller IS device-bound (PG), the device
/// row's token is kept in sync as well for device-change flows that read it directly.
/// </summary>
public sealed record RegisterFcmTokenCommand(Guid UserId, Guid? CallerDeviceRowId, string Token)
    : IRequest<Result>;

internal sealed class RegisterFcmTokenCommandHandler : IRequestHandler<RegisterFcmTokenCommand, Result>
{
    private readonly IAppDbContext _db;
    public RegisterFcmTokenCommandHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result> Handle(RegisterFcmTokenCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, "Thiếu FCM token."));

        var token = command.Token.Trim();

        // 1) Upsert push_tokens (source of truth for push delivery). Re-point the install's token
        //    to the current account so notifications only reach who is signed in on that device.
        var existing = await _db.PushTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
        if (existing is null)
            _db.PushTokens.Add(PushToken.Create(command.UserId, token));
        else if (existing.UserId != command.UserId)
            existing.AssignTo(command.UserId);

        // 2) Best-effort: keep the device row's token in sync when the caller is device-bound (PG).
        var device = await ResolveDeviceAsync(command.UserId, command.CallerDeviceRowId, ct);
        if (device is not null && device.Status is DeviceStatus.Active or DeviceStatus.PendingApproval)
            device.UpdateFcmToken(token);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<UserDevice?> ResolveDeviceAsync(
        Guid userId,
        Guid? callerDeviceRowId,
        CancellationToken ct)
    {
        if (callerDeviceRowId is { } rowId && rowId != Guid.Empty)
        {
            var byClaim = await _db.UserDevices
                .FirstOrDefaultAsync(d => d.Id == rowId && d.UserId == userId, ct);
            if (byClaim is not null)
                return byClaim;
        }

        return await _db.UserDevices
            .Where(d => d.UserId == userId && d.Status == DeviceStatus.Active)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
