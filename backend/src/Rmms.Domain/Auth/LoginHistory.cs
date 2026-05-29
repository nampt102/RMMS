using System.Net;
using Rmms.Domain.Common;

namespace Rmms.Domain.Auth;

/// <summary>
/// Append-only login attempt log per <c>04-data-model.md</c>.
/// One row per login attempt (success OR failure).
///
/// Separate from <c>audit_log</c> because:
///   - login_history rows are written on every attempt (even failures) and are
///     query-heavy for "show me my recent logins" UX.
///   - audit_log is for compliance + Admin investigation, written more selectively
///     and queried less frequently.
///
/// Not <see cref="AuditableEntity"/> — this IS a log.
/// </summary>
public sealed class LoginHistory : Entity
{
    public Guid UserId { get; private set; }

    /// <summary>FK to <c>user_devices.id</c>. Nullable for failed attempts before device is known.</summary>
    public Guid? DeviceId { get; private set; }

    public IPAddress? IpAddress { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;

    public bool Success { get; private set; }

    /// <summary>
    /// Why it failed. Examples: <c>invalid_credentials</c>, <c>device_not_authorized</c>,
    /// <c>account_inactive</c>, <c>account_locked</c>, <c>email_not_verified</c>.
    /// Null on success.
    /// </summary>
    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private LoginHistory() { }

    public static LoginHistory RecordSuccess(Guid userId, Guid deviceId, IPAddress? ip, string userAgent, DateTimeOffset at) =>
        new()
        {
            UserId = userId,
            DeviceId = deviceId,
            IpAddress = ip,
            UserAgent = (userAgent ?? string.Empty).Trim(),
            Success = true,
            FailureReason = null,
            CreatedAt = at,
        };

    public static LoginHistory RecordFailure(Guid userId, Guid? deviceId, IPAddress? ip, string userAgent, string failureReason, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);
        return new LoginHistory
        {
            UserId = userId,
            DeviceId = deviceId,
            IpAddress = ip,
            UserAgent = (userAgent ?? string.Empty).Trim(),
            Success = false,
            FailureReason = failureReason,
            CreatedAt = at,
        };
    }
}
