using Rmms.Domain.Enums;

namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Multi-channel notification dispatcher (M14). Persists the durable in-app row and
/// fans out to push (FCM) + email per the spec's channel flags (CR-3).
///
/// Contract: <see cref="NotifyAsync"/> ADDS the in-app <c>Notification</c> to the
/// unit of work but does NOT call SaveChanges — the caller owns the transaction, so
/// the in-app row commits atomically with the business change. Push/email are
/// best-effort side effects (failures are logged, never thrown) — a notification must
/// never roll back or break the originating action.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(Guid userId, NotificationSpec spec, CancellationToken ct = default);
}

/// <summary>
/// A channel-agnostic, bilingual notification request. The service resolves the
/// recipient's <c>PreferredLanguage</c> to pick the title/body before persisting.
/// </summary>
public sealed record NotificationSpec(
    NotificationType Type,
    string TitleVi,
    string TitleEn,
    string BodyVi,
    string BodyEn,
    /// <summary>Deep link + entity refs serialised into the in-app row's <c>data</c> (jsonb).</summary>
    IReadOnlyDictionary<string, string>? Data = null,
    /// <summary>Send a push (FCM) when the recipient has a registered token.</summary>
    bool Push = true,
    /// <summary>Send an email as well (used for high-signal events per CR-3).</summary>
    bool Email = false);
