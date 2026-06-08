using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;
using Rmms.Domain.Notifications;

namespace Rmms.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="INotificationService"/> (M14). Persists the in-app row (added to the
/// unit of work, committed by the caller) and best-effort fans out to push + email.
///
/// Phase 1A "basic": in-app is durable; push uses the configured <see cref="IPushSender"/>
/// (logging by default); email reuses <see cref="IEmailSender"/>. Hangfire retry/batching
/// (M14 edge cases) is deferred to Phase 1B.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly IPushSender _push;
    private readonly IEmailSender _email;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<NotificationService> _log;

    public NotificationService(
        IAppDbContext db,
        IPushSender push,
        IEmailSender email,
        IDateTimeProvider clock,
        ILogger<NotificationService> log)
    {
        _db = db;
        _push = push;
        _email = email;
        _clock = clock;
        _log = log;
    }

    public async Task NotifyAsync(Guid userId, NotificationSpec spec, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            _log.LogWarning("Notification skipped — user {UserId} not found.", userId);
            return;
        }

        var isVi = !string.Equals(user.PreferredLanguage, "en", StringComparison.OrdinalIgnoreCase);
        var title = isVi ? spec.TitleVi : spec.TitleEn;
        var body = isVi ? spec.BodyVi : spec.BodyEn;
        var dataJson = spec.Data is { Count: > 0 } ? JsonSerializer.Serialize(spec.Data) : null;

        // Resolve the active device's FCM token (PG has exactly one active device, BR-105).
        var fcmToken = spec.Push
            ? await _db.UserDevices.AsNoTracking()
                .Where(d => d.UserId == userId
                    && d.Status == DeviceStatus.Active
                    && d.FcmToken != null)
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .Select(d => d.FcmToken)
                .FirstOrDefaultAsync(ct)
            : null;

        var channels = new List<string> { "in_app" };
        if (spec.Push && !string.IsNullOrWhiteSpace(fcmToken)) channels.Add("push");
        if (spec.Email) channels.Add("email");

        // 1) Durable in-app row — committed by the caller's SaveChanges (atomic with the business change).
        var notification = Notification.Create(userId, spec.Type, title, body, dataJson, channels, _clock.UtcNow);
        _db.Notifications.Add(notification);

        // 2) Best-effort push (failures logged, never thrown).
        if (spec.Push && !string.IsNullOrWhiteSpace(fcmToken))
        {
            try
            {
                await _push.SendAsync(new PushMessage(fcmToken!, title, body, spec.Data), ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Push notification failed for user {UserId} (type {Type}).", userId, spec.Type);
            }
        }

        // 3) Best-effort email.
        if (spec.Email)
        {
            try
            {
                var html = $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p>";
                await _email.SendAsync(
                    new EmailMessage(user.Email, user.FullName, title, body, html, isVi ? "vi" : "en"), ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Email notification failed for user {UserId} (type {Type}).", userId, spec.Type);
            }
        }
    }
}
