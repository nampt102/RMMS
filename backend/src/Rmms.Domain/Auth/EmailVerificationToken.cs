using Rmms.Domain.Common;

namespace Rmms.Domain.Auth;

/// <summary>
/// Single-use email verification token (TTL 24h) per M01 spec.
/// Token bytes random (256-bit), only hash stored in DB.
/// </summary>
public sealed class EmailVerificationToken : Entity
{
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hex of the raw token shown in email URL.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private EmailVerificationToken() { }

    public static EmailVerificationToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new EmailVerificationToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };
    }

    public bool IsValid(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void MarkUsed(DateTimeOffset at)
    {
        if (UsedAt is not null)
        {
            throw new InvalidOperationException("Email verification token already used.");
        }
        UsedAt = at;
    }
}
