using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Devices.GetPendingDevices;

/// <summary>
/// Lists device-change requests awaiting approval (BR-106).
/// Admins see all pending requests; Leaders see only requests from PGs they actively
/// manage (via M03 <c>user_leader_assignments</c>, <c>effective_to IS NULL</c>).
/// </summary>
public sealed record GetPendingDevicesQuery(Guid CallerUserId, bool IsAdmin)
    : IRequest<Result<IReadOnlyList<PendingDeviceDto>>>;

public sealed record PendingDeviceDto(
    Guid DeviceId,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string UserRole,
    string DeviceName,
    string Os,
    string OsVersion,
    string AppVersion,
    DateTimeOffset RequestedAt);
