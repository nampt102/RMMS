using System.Collections.Concurrent;
using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>Captures realtime pushes so tests can assert the SignalR fan-out.</summary>
internal sealed class FakeRealtimeNotifier : IRealtimeNotifier
{
    public ConcurrentBag<(Guid UserId, RealtimeNotification Payload)> Sent { get; } = new();

    public Task PushNotificationAsync(Guid userId, RealtimeNotification payload, CancellationToken ct = default)
    {
        Sent.Add((userId, payload));
        return Task.CompletedTask;
    }
}
