using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.UnitTests.Common;

internal sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public UserRole? Role { get; init; }
    public Guid? DeviceId { get; init; }
    public bool IsAuthenticated => UserId is not null;
}
