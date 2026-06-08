using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Rmms.Api.Hubs;

/// <summary>
/// Maps a SignalR connection to our user id. The JWT keeps claim names as issued
/// (<c>MapInboundClaims = false</c>), so the id lives in <c>sub</c> (or NameIdentifier as a
/// fallback) — mirror <c>HttpContextCurrentUser</c> so <c>Clients.User(id)</c> targets correctly.
/// </summary>
public sealed class HubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? connection.User?.FindFirst("sub")?.Value;
}
