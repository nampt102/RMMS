using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Notifications;

namespace Rmms.Api.Controllers;

/// <summary>
/// In-app notifications + FCM token registration (M14). Any authenticated user reads
/// their own inbox; push targets the caller's active device.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Paginated inbox + unread badge count.</summary>
    [HttpGet("notifications/me")]
    public async Task<IActionResult> Mine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetMyNotificationsQuery(userId, page, pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Mark one notification read.</summary>
    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new MarkNotificationReadCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Mark all of the caller's notifications read.</summary>
    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new MarkAllNotificationsReadCommand(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(new { updated = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Register / refresh the FCM push token for the caller's active device.</summary>
    [HttpPut("users/me/fcm-token")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenBody body, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new RegisterFcmTokenCommand(userId, body.Token), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}

public sealed record RegisterFcmTokenBody(string Token);
