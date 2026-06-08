using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Notifications;

/// <summary>
/// In-app notification row (M14). One row per delivered notification to one user.
/// Spec: <c>knowledge-base/04-data-model.md</c> (notifications table) and
/// <c>modules/M14-news-notification.md</c>. Retention: 90 days (CR-4).
///
/// The row is the durable in-app record + read state; the push/email fan-out is
/// best-effort and recorded in <see cref="ChannelsSent"/>. Title/Body are stored
/// already localised to the recipient's <c>PreferredLanguage</c>.
/// </summary>
public sealed class Notification : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    /// <summary>JSON payload (deep link + entity refs). Null when there is no payload.</summary>
    public string? Data { get; private set; }

    /// <summary>Channels actually attempted, e.g. <c>["in_app","push","email"]</c>.</summary>
    public List<string> ChannelsSent { get; private set; } = new();

    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification() { } // EF Core

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? data,
        IEnumerable<string> channelsSent,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new Notification
        {
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Body = body?.Trim() ?? string.Empty,
            Data = string.IsNullOrWhiteSpace(data) ? null : data,
            ChannelsSent = channelsSent?.ToList() ?? new List<string>(),
            IsRead = false,
            CreatedAt = now,
        };
    }

    public void MarkRead(DateTimeOffset now)
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = now;
    }
}
