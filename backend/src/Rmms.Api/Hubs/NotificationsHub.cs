using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Rmms.Api.Hubs;

/// <summary>
/// Realtime notification channel (M14). Authenticated users are automatically placed in a
/// per-user group keyed by their id (via <c>IUserIdProvider</c>), so the server pushes with
/// <c>Clients.User(userId)</c>. The client only receives a <c>"notification"</c> event and
/// reacts (toast + refetch) — there are no client→server methods.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
}
