namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Sends a push notification to a single device token (FCM).
///
/// Implementations:
///   - <c>Rmms.Infrastructure.Notifications.LoggingPushSender</c> — Dev / Phase 1A default
///     (logs the payload; no network). Selected when <c>Push:Provider</c> is unset/console.
///   - <c>Rmms.Infrastructure.Notifications.FcmPushSender</c> — real FCM HTTP v1 (Phase 1B,
///     needs a service-account credential). Selected when <c>Push:Provider=fcm</c>.
///
/// Best-effort: callers swallow + log failures so a push never breaks the originating action.
/// </summary>
public interface IPushSender
{
    Task SendAsync(PushMessage message, CancellationToken ct = default);
}

public sealed record PushMessage(
    string DeviceToken,
    string Title,
    string Body,
    /// <summary>Data payload — typically a <c>deepLink</c> (e.g. <c>rmms://approvals/123</c>) + refs.</summary>
    IReadOnlyDictionary<string, string>? Data = null);
