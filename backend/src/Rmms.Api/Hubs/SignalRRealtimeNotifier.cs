using Microsoft.AspNetCore.SignalR;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Api.Hubs;

/// <summary>
/// SignalR-backed <see cref="IRealtimeNotifier"/> — pushes the <c>"notification"</c> event to the
/// target user's connections via <see cref="NotificationsHub"/>. Registered by the API host to
/// override the Infrastructure no-op default.
/// </summary>
internal sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hub) => _hub = hub;

    public Task PushNotificationAsync(Guid userId, RealtimeNotification payload, CancellationToken ct = default) =>
        _hub.Clients.User(userId.ToString()).SendAsync("notification", payload, ct);
}
