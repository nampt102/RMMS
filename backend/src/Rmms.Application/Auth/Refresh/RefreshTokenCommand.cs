using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Refresh;

/// <summary>
/// Rotate (access_token, refresh_token) pair. Old refresh token is revoked atomically.
/// On reuse of an already-revoked token → revoke ALL user's active refresh tokens
/// + emit <see cref="Rmms.Domain.Enums.AuditAction.AuthRefreshReused"/> per
/// <c>sprints/sprint-01.md</c> R-3 mitigation.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;
