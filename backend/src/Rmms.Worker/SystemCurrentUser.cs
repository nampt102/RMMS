using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.Worker;

/// <summary>
/// Used inside Hangfire jobs where there is no HTTP context. Treat the actor as "system".
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public string? Email => "system@rmms.local";
    public UserRole? Role => UserRole.Admin;
    public Guid? DeviceId => null;
    public bool IsAuthenticated => false;
}
