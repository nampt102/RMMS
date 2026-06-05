using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Devices.GetPendingDevices;

/// <summary>
/// Lists device-change requests awaiting approval (BR-106). Sprint 02 slice: Admin-scoped.
/// Leader-scoped listing (only the leader's assigned PGs) is enabled once M03
/// <c>user_leader_assignments</c> exists.
/// </summary>
public sealed record GetPendingDevicesQuery : IRequest<Result<IReadOnlyList<PendingDeviceDto>>>;

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
