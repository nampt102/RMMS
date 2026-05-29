using Rmms.Domain.Common;

namespace Rmms.Domain.Auth;

/// <summary>
/// Single-use password reset token (TTL 24h) per M01 spec.
/// Same pattern as <see cref="EmailVerificationToken"/> but separate table so
/// we can have different lifetimes / TTL knobs without complicating the schema.
/// </summary>
public sealed class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new PasswordResetToken
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
            throw new InvalidOperationException("Password reset token already used.");
        }
        UsedAt = at;
    }
}
