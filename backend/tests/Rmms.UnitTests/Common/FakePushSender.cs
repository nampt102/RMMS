using System.Collections.Concurrent;
using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>Captures push sends so tests can assert the FCM fan-out.</summary>
internal sealed class FakePushSender : IPushSender
{
    public ConcurrentBag<PushMessage> Sent { get; } = new();

    public Task SendAsync(PushMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
