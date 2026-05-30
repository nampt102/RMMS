using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Me;

/// <summary>
/// Returns the status of the device tied to the current access token (M01 / M02 skeleton).
/// Sprint 01 ships the authenticated read; Sprint 02 adds the pending-device polling variant
/// (callable by a not-yet-approved device) + push/in-app notification.
/// </summary>
public sealed record GetDeviceStatusQuery(Guid UserId, Guid? DeviceId)
    : IRequest<Result<DeviceStatusDto>>;

/// <summary>
/// <see cref="Status"/> is the snake_case <c>DeviceStatus</c> (<c>active</c> / <c>pending_approval</c> /
/// <c>rejected</c> / <c>replaced</c>), or <c>none</c> for web users (no device-bound token), or
/// <c>unknown</c> if the device row is missing.
/// </summary>
public sealed record DeviceStatusDto(
    string Status,
    Guid? DeviceId,
    string? DeviceName,
    DateTimeOffset? RequestedAt);
