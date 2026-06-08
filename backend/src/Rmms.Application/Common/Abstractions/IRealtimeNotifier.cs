namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Pushes a lightweight realtime signal to a connected user (SignalR). Complements the
/// durable in-app row + FCM push: web clients (no FCM) get a live badge/toast and can
/// refetch. Best-effort — <see cref="INotificationService"/> swallows failures.
///
/// The default registration is a no-op (Worker / tests); the API host overrides it with a
/// SignalR-backed implementation.
/// </summary>
public interface IRealtimeNotifier
{
    Task PushNotificationAsync(Guid userId, RealtimeNotification payload, CancellationToken ct = default);
}

/// <summary>Minimal realtime payload — enough to toast + trigger a refetch on the client.</summary>
public sealed record RealtimeNotification(string Type, string Title, string Body);
