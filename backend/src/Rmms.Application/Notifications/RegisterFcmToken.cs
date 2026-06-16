using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Notifications;

/// <summary>
/// Register / refresh the FCM push token for the caller's device (M14, BR-105).
/// Resolves the row via the JWT <c>device_id</c> claim first, then falls back to the
/// user's active device. Pending-approval rows are updated too so device-change pushes
/// can reach the waiting install. Non-device-bound callers (web / empty claim) are a
/// no-op success.
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

        var device = await ResolveDeviceAsync(command.UserId, command.CallerDeviceRowId, ct);
        if (device is null)
        {
            // Leader / web sessions carry an empty device_id claim — nothing to update.
            if (command.CallerDeviceRowId is Guid.Empty)
                return Result.Success();

            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy thiết bị đang hoạt động."));
        }

        if (device.Status is DeviceStatus.Rejected or DeviceStatus.Replaced)
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy thiết bị đang hoạt động."));

        if (device.Status is not (DeviceStatus.Active or DeviceStatus.PendingApproval))
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy thiết bị đang hoạt động."));

        device.UpdateFcmToken(command.Token);
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
