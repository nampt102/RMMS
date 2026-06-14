using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.News;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.News;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin News editor (M14, AC-33/34): create bilingual drafts, assign to role/user, publish
/// (notifies recipients, CR-2), edit, and delete. Important news requires reader confirmation.
/// </summary>
[ApiController]
[Route("api/v1/admin/news")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminNewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminNewsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new AdminGetNewsQuery(), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNewsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateNewsCommand(
            request.TitleVi, request.TitleEn, request.ContentVi, request.ContentEn, request.Category, request.IsImportant), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateNewsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateNewsCommand(
            id, request.TitleVi, request.TitleEn, request.ContentVi, request.ContentEn, request.Category, request.IsImportant), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/assignments")]
    public async Task<IActionResult> Assign([FromRoute] Guid id, [FromBody] AssignNewsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignNewsCommand(id, request.Role, request.UserId), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new PublishNewsCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteNewsCommand(id), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
