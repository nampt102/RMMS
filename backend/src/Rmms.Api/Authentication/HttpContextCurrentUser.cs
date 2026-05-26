using System.Security.Claims;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.Api.Authentication;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from the ambient JWT claims on every HTTP request.
/// Claim shape per <c>05-api-conventions.md</c> JWT payload: sub, email, role, device_id.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub"),
            out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue("role"), ignoreCase: true, out var role)
            ? role : null;

    public Guid? DeviceId =>
        Guid.TryParse(Principal?.FindFirstValue("device_id"), out var id) ? id : null;
}
