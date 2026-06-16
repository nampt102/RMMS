using Rmms.Domain.Common;

namespace Rmms.Domain.Notifications;

/// <summary>
/// Push (FCM) token for a user's install (M14, CR-3). Deliberately decoupled from
/// <c>UserDevice</c> / the BR-105 device-lock — that lock is PG-only, so binding push
/// tokens to it left Leader/BUH (who use the app but carry no active device row) with
/// nowhere to store a token and therefore no push delivery.
///
/// One row per FCM token (the token identifies a single app install). The same install
/// shared across accounts (PG logs out → Leader logs in) re-points the token to whoever
/// last registered it via <see cref="AssignTo"/>, so a notification only ever reaches the
/// account currently signed in on that device.
/// </summary>
public sealed class PushToken : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }

    /// <summary>The FCM registration token. Unique across installs.</summary>
    public string Token { get; private set; } = string.Empty;

    private PushToken() { } // EF Core

    public static PushToken Create(Guid userId, string token)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new PushToken
        {
            UserId = userId,
            Token = token.Trim(),
        };
    }

    /// <summary>Re-point this token to the current user (shared install / re-login).</summary>
    public void AssignTo(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        UserId = userId;
    }
}
