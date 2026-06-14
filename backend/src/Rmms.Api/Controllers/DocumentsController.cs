using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Documents;

namespace Rmms.Api.Controllers;

/// <summary>
/// Mobile Document Center surface (M13, AC-31/32): list documents accessible to the caller
/// (role/user OR logic) and mint a short-lived signed download URL. Private-file downloads are
/// audited (CR-1).
/// </summary>
[ApiController]
[Route("api/v1/documents")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public DocumentsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public async Task<IActionResult> MyDocuments([FromQuery] string? search, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new GetMyDocumentsQuery(userId, role, search), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new GetDocumentDownloadUrlQuery(id, userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(new { url = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
