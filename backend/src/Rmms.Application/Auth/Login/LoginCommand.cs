using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Login;

/// <summary>
/// Login command per <c>05-api-conventions.md</c> §Authentication + M01 spec.
///
/// Device check (BR-105):
///   - First device for user → auto-approved active.
///   - Same active device → reuse, touch <c>last_used_at</c>.
///   - Different device while another is active → returns
///     <see cref="Rmms.Shared.Errors.ErrorCodes.DeviceNotAuthorized"/> (403) +
///     creates a pending_approval row for Sprint 02 Leader/Admin approval UI.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    LoginDeviceInfo Device)
    : IRequest<Result<LoginResponse>>;

/// <summary>
/// Device fingerprint sent by mobile / web on every login.
///
/// <see cref="DeviceId"/> is the same value as the <c>X-Device-Id</c> header
/// (mobile generates UUID v4 on first install and stores in secure storage).
/// </summary>
public sealed record LoginDeviceInfo(
    string DeviceId,
    string DeviceName,
    string Os,
    string OsVersion,
    string AppVersion,
    string? FcmToken);
