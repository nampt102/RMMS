using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.News;

namespace Rmms.Api.Controllers;

/// <summary>
/// Mobile News surface (M14, AC-33/34): list published news assigned to the caller (with read /
/// confirm state), mark read, and confirm important news.
/// </summary>
[ApiController]
[Route("api/v1/news")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class NewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public NewsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public async Task<IActionResult> MyNews(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new GetMyNewsQuery(userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> Read([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new MarkNewsReadCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new ConfirmNewsCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
