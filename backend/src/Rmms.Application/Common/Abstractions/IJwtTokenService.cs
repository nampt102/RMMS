using Rmms.Domain.Enums;

namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Issues + validates JWT access tokens per <c>05-api-conventions.md</c>.
/// Refresh tokens are opaque random bytes — see <see cref="IRefreshTokenGenerator"/>.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Issue an access token (HS256, 15-minute lifetime by default).
    /// Payload: <c>sub</c>=userId, <c>email</c>, <c>role</c>, <c>device_id</c>, <c>iat</c>, <c>exp</c>.
    /// </summary>
    IssuedAccessToken IssueAccess(Guid userId, string email, UserRole role, Guid deviceId, DateTimeOffset now);
}

/// <summary>Result of access-token issuance.</summary>
public readonly record struct IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
