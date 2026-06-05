using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Devices.GetMyDevice;

/// <summary>Returns the caller's active device, plus any pending device-change request they have.</summary>
public sealed record GetMyDeviceQuery(Guid UserId) : IRequest<Result<MyDeviceDto>>;

public sealed record MyDeviceDto(
    DeviceSummary? Active,
    DeviceSummary? Pending);

public sealed record DeviceSummary(
    Guid Id,
    string DeviceId,
    string DeviceName,
    string Os,
    string Status,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset CreatedAt);
