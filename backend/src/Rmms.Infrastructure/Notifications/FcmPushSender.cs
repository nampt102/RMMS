using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Notifications;

/// <summary>
/// Real <see cref="IPushSender"/> backed by the Firebase Admin SDK (FCM HTTP v1).
/// Selected when <c>Push:Provider=fcm</c>; the default <see cref="FirebaseApp"/> is
/// initialised once at startup with the service-account credential (see DI).
///
/// Best-effort by contract: <see cref="NotificationService"/> wraps the call in try/catch,
/// so an unregistered/expired token (FCM returns NOT_FOUND / UNREGISTERED) never breaks
/// the originating action — it is logged and the in-app notification still stands.
/// </summary>
internal sealed class FcmPushSender : IPushSender
{
    private readonly ILogger<FcmPushSender> _log;

    public FcmPushSender(ILogger<FcmPushSender> log) => _log = log;

    public async Task SendAsync(PushMessage message, CancellationToken ct = default)
    {
        var fcm = new Message
        {
            Token = message.DeviceToken,
            Notification = new Notification
            {
                Title = message.Title,
                Body = message.Body,
            },
            // Data must be string→string; the deep link travels here for tap-routing.
            Data = message.Data is { Count: > 0 }
                ? new Dictionary<string, string>(message.Data)
                : null,
        };

        var id = await FirebaseMessaging.DefaultInstance.SendAsync(fcm, ct);
        _log.LogDebug("FCM message sent: {MessageId}", id);
    }
}
