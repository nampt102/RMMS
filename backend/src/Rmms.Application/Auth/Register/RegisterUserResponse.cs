namespace Rmms.Application.Auth.Register;

/// <summary>
/// Returned to client after successful <see cref="RegisterUserCommand"/>.
/// Does NOT include access/refresh tokens — user must verify email + login first.
/// </summary>
public sealed record RegisterUserResponse(
    Guid UserId,
    string Email,
    string Status);
