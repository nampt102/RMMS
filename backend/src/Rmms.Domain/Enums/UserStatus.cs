namespace Rmms.Domain.Enums;

/// <summary>
/// User account status per <c>04-data-model.md</c>.
/// Stored as lowercase string in DB.
/// </summary>
public enum UserStatus
{
    /// <summary>Self-registered but email not yet verified — cannot login.</summary>
    PendingEmailVerify = 1,

    /// <summary>Active account, normal operation.</summary>
    Active = 2,

    /// <summary>Deactivated by Admin. Refresh tokens revoked. Cannot login.</summary>
    Inactive = 3,
}
