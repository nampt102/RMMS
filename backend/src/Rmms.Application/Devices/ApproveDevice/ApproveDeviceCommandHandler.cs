using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Devices.ApproveDevice;

internal sealed class ApproveDeviceCommandHandler : IRequestHandler<ApproveDeviceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ApproveDeviceCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(ApproveDeviceCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var device = await _db.UserDevices.SingleOrDefaultAsync(d => d.Id == command.DeviceId, ct);
        if (device is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy yêu cầu thiết bị."));
        }

        if (device.Status != DeviceStatus.PendingApproval)
        {
            return Result.Failure(Error.Conflict(ErrorCodes.ApprovalNotPending, "Yêu cầu thiết bị không ở trạng thái chờ duyệt."));
        }

        // Replace the user's current active device (if any) + revoke its refresh tokens (BR-106).
        var currentActive = await _db.UserDevices
            .SingleOrDefaultAsync(d => d.UserId == device.UserId && d.Status == DeviceStatus.Active, ct);

        if (currentActive is not null)
        {
            currentActive.MarkReplaced();

            var oldTokens = await _db.RefreshTokens
                .Where(t => t.UserId == device.UserId && t.DeviceId == currentActive.Id && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var token in oldTokens)
            {
                token.Revoke(now);
            }
        }

        device.Approve(command.ApproverUserId, now);

        await _audit.RecordAsync(
            action: AuditAction.DeviceApproved,
            targetEntity: "user_device",
            targetId: device.Id,
            metadata: new
            {
                user_id = device.UserId,
                device_id = device.DeviceId,
                replaced_device_id = currentActive?.Id,
            },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
