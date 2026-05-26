using Rmms.Domain.Enums;

namespace Rmms.Application.Common.Interfaces;

/// <summary>
/// Ambient info about the caller. Implemented in Api layer using <c>IHttpContextAccessor</c> + JWT claims.
/// Per <c>05-api-conventions.md</c> JWT payload: sub, email, role, device_id.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    Guid? DeviceId { get; }
    bool IsAuthenticated { get; }
}
