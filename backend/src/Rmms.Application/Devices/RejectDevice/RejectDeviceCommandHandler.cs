using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Devices.RejectDevice;

internal sealed class RejectDeviceCommandHandler : IRequestHandler<RejectDeviceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public RejectDeviceCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(RejectDeviceCommand command, CancellationToken ct)
    {
        var device = await _db.UserDevices.SingleOrDefaultAsync(d => d.Id == command.DeviceId, ct);
        if (device is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy yêu cầu thiết bị."));
        }

        if (device.Status != DeviceStatus.PendingApproval)
        {
            return Result.Failure(Error.Conflict(ErrorCodes.ApprovalNotPending, "Yêu cầu thiết bị không ở trạng thái chờ duyệt."));
        }

        // Leader-scoping (BR-106): a Leader may only reject devices of PGs they actively manage.
        if (!command.ApproverIsAdmin)
        {
            var manages = await _db.UserLeaderAssignments.AnyAsync(
                a => a.LeaderUserId == command.ApproverUserId && a.PgUserId == device.UserId && a.EffectiveTo == null, ct);
            if (!manages)
            {
                return Result.Failure(Error.Forbidden(ErrorCodes.NotApprover, "Bạn không quản lý PG này nên không thể từ chối thiết bị."));
            }
        }

        device.Reject(command.ApproverUserId, _clock.UtcNow);

        await _audit.RecordAsync(
            action: AuditAction.DeviceRejected,
            targetEntity: "user_device",
            targetId: device.Id,
            metadata: new { user_id = device.UserId, device_id = device.DeviceId, reason = command.Reason },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
