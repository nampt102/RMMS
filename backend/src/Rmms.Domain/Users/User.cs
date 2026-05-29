using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Users;

/// <summary>
/// User aggregate root.
///
/// Spec: <c>knowledge-base/04-data-model.md</c> (users table) and
/// <c>knowledge-base/modules/M01-identity-access.md</c>.
///
/// Invariants (enforced via factory methods + setters):
///   - <c>Email</c> is always stored lowercase, trimmed.
///   - <c>PasswordHash</c> non-null in Phase 1 (Phase 2 may allow SSO-only users).
///   - <c>Role</c> immutable after creation (re-create user if role change needed — different responsibility).
///   - <c>Status</c> transitions via explicit domain methods (not setter).
///
/// Future hooks (per sprint-01.md §9 extensibility, all nullable / default-safe):
///   - <c>ExternalProvider</c> + <c>ExternalId</c>: Phase 2 SSO (Google / Microsoft).
///   - <c>MfaEnabled</c> + <c>MfaSecretExternalId</c>: Phase 2 MFA.
///   - <c>FaceEnrolledAt</c> + <c>FaceTemplateExternalId</c>: M06 face verification.
/// </summary>
public sealed class User : AggregateRoot
{
    /// <summary>Email — always lowercase, unique across active users.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>BCrypt cost 12 hash. See <c>IPasswordHasher</c>.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }

    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>ISO 639-1 language code: <c>vi</c> (default) or <c>en</c>.</summary>
    public string PreferredLanguage { get; private set; } = "vi";

    // ----- Face verification (M06) hooks -----
    public DateTimeOffset? FaceEnrolledAt { get; private set; }
    public string? FaceTemplateExternalId { get; private set; }

    // ----- External identity (Phase 2 SSO) hooks -----
    public string? ExternalProvider { get; private set; }
    public string? ExternalId { get; private set; }

    // ----- MFA (Phase 2) hooks -----
    public bool MfaEnabled { get; private set; }
    public string? MfaSecretExternalId { get; private set; }

    // ----- EF Core constructor (private; use factories) -----
    private User() { }

    /// <summary>Factory: PG self-registration via email. Status starts <c>PendingEmailVerify</c>.</summary>
    public static User Register(string email, string passwordHash, string fullName, string? phone = null, string preferredLanguage = "vi")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Phone = phone?.Trim(),
            Role = UserRole.Pg,
            Status = UserStatus.PendingEmailVerify,
            PreferredLanguage = NormalizeLanguage(preferredLanguage),
        };
    }

    /// <summary>Factory: Admin creates Leader/BUH/Admin. Status starts <c>Active</c> (no email verify needed).</summary>
    public static User CreateByAdmin(string email, string passwordHash, string fullName, UserRole role, string? phone = null, string preferredLanguage = "vi")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        if (role == UserRole.Pg)
        {
            throw new InvalidOperationException(
                "PG accounts must self-register via Register() per BR-101. " +
                "Admin can only create Leader/BUH/Admin.");
        }

        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Phone = phone?.Trim(),
            Role = role,
            Status = UserStatus.Active,
            EmailVerifiedAt = now, // admin-created accounts are pre-verified
            PreferredLanguage = NormalizeLanguage(preferredLanguage),
        };
    }

    // ----- Domain methods -----

    public void VerifyEmail(DateTimeOffset at)
    {
        if (Status != UserStatus.PendingEmailVerify)
        {
            throw new InvalidOperationException(
                $"Cannot verify email — user is already {Status}.");
        }

        EmailVerifiedAt = at;
        Status = UserStatus.Active;
    }

    public void ChangePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
    }

    public void Activate()
    {
        if (Status == UserStatus.PendingEmailVerify)
        {
            throw new InvalidOperationException(
                "Cannot activate a user with unverified email. Verify first.");
        }
        Status = UserStatus.Active;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
    }

    public void RecordLogin(DateTimeOffset at)
    {
        LastLoginAt = at;
    }

    public void UpdateProfile(string? fullName = null, string? phone = null, string? preferredLanguage = null)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            FullName = fullName.Trim();
        }

        if (phone is not null) // explicit null means "clear"
        {
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(preferredLanguage))
        {
            PreferredLanguage = NormalizeLanguage(preferredLanguage);
        }
    }

    /// <summary>M06 hook — call when face enrollment succeeds at FPT.AI.</summary>
    public void RecordFaceEnrollment(string templateExternalId, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateExternalId);
        FaceTemplateExternalId = templateExternalId;
        FaceEnrolledAt = at;
    }

    // ----- Helpers -----

    private static string NormalizeLanguage(string lang)
    {
        var normalized = lang.Trim().ToLowerInvariant();
        return normalized is "vi" or "en" ? normalized : "vi";
    }
}
