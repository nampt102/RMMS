using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Notifications;

/// <summary>
/// Phase 1A default <see cref="IPushSender"/> — logs the push payload instead of calling FCM.
/// Lets the full notification flow (in-app + routing + deep link) be developed and tested
/// without a Firebase credential. Swap to a real FCM HTTP v1 sender in Phase 1B via
/// <c>Push:Provider=fcm</c> (see <see cref="IPushSender"/> docs).
/// </summary>
internal sealed class LoggingPushSender : IPushSender
{
    private readonly ILogger<LoggingPushSender> _log;

    public LoggingPushSender(ILogger<LoggingPushSender> log) => _log = log;

    public Task SendAsync(PushMessage message, CancellationToken ct = default)
    {
        _log.LogInformation(
            "[PUSH] token={Token} title={Title} body={Body} data={Data}",
            Mask(message.DeviceToken),
            message.Title,
            message.Body,
            message.Data is null ? "{}" : string.Join(",", message.Data.Select(kv => $"{kv.Key}={kv.Value}")));
        return Task.CompletedTask;
    }

    private static string Mask(string token) =>
        token.Length <= 8 ? "****" : $"{token[..4]}…{token[^4..]}";
}
