using Rmms.Domain.Common;

namespace Rmms.Domain.Auth;

/// <summary>
/// Refresh token row per <c>04-data-model.md</c> and <c>05-api-conventions.md</c>.
///
/// - Stored as SHA-256 hash (never plaintext).
/// - Lifetime 30 days from creation.
/// - Rotated on every <c>POST /auth/refresh</c>: old <see cref="Revoke"/>d, new issued.
/// - Reuse detection: if a revoked token is presented again, ALL active tokens for the user are revoked
///   (handled by the application service, not the entity).
///
/// Not <see cref="AuditableEntity"/> because tokens are short-lived and we don't soft-delete them
/// (Hangfire cleanup job hard-deletes expired rows).
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }

    /// <summary>SHA-256 hex of the raw token. Lookup index targets this column.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// When this token was rotated by another, link to the replacement so we can trace chains
    /// (helpful for reuse detection forensics).
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(Guid userId, Guid deviceId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken
        {
            UserId = userId,
            DeviceId = deviceId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };
    }

    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && now < ExpiresAt;

    public void Revoke(DateTimeOffset at)
    {
        RevokedAt ??= at;
    }

    public void MarkRotatedBy(Guid newTokenId, DateTimeOffset at)
    {
        ReplacedByTokenId = newTokenId;
        Revoke(at);
    }
}
