using System.Collections.Concurrent;
using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>Captures notification calls so tests can assert who was notified, with what spec.</summary>
internal sealed class FakeNotificationService : INotificationService
{
    public ConcurrentBag<(Guid UserId, NotificationSpec Spec)> Sent { get; } = new();

    public Task NotifyAsync(Guid userId, NotificationSpec spec, CancellationToken ct = default)
    {
        Sent.Add((userId, spec));
        return Task.CompletedTask;
    }
}
