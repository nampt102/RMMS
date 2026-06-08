using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="IRealtimeNotifier"/> — does nothing. Used by hosts without SignalR
/// (Worker / tests). The API host registers a SignalR-backed override after AddInfrastructure.
/// </summary>
internal sealed class NoOpRealtimeNotifier : IRealtimeNotifier
{
    public Task PushNotificationAsync(Guid userId, RealtimeNotification payload, CancellationToken ct = default) =>
        Task.CompletedTask;
}
