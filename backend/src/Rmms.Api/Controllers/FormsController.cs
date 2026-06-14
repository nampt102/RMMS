using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Forms;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Forms;

namespace Rmms.Api.Controllers;

/// <summary>
/// Mobile fill surface for the Form Engine (M10, AC-22): list my assigned forms, get a form's
/// schema to render, and submit (idempotent for offline retries, AC-23). Any authenticated user;
/// assignment resolution scopes what each filler can see.
/// </summary>
[ApiController]
[Route("api/v1/forms")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class FormsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public FormsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public async Task<IActionResult> MyForms(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new GetMyFormsQuery(userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new GetFormForFillQuery(id, userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment([FromRoute] Guid id, IFormFile file, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        if (file is null || file.Length == 0)
        {
            return ResultMapping.Failure(
                Rmms.Domain.Common.Error.Validation(Rmms.Shared.Errors.ErrorCodes.ValidationFailed, "Tệp rỗng."),
                HttpContext.TraceIdentifier);
        }
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var result = await _mediator.Send(new UploadFormAttachmentCommand(
            id, userId, new Rmms.Application.Common.Abstractions.PhotoUpload(file.FileName, file.ContentType, ms.ToArray())), ct);
        return result.IsSuccess ? ResultMapping.Created(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit([FromRoute] Guid id, [FromBody] SubmitFormRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new SubmitFormCommand(
            id, userId, role,
            request.Answers.GetRawText(),
            request.Attachments?.GetRawText(),
            request.StoreId, request.TimeSpentSeconds, request.ClientIdempotencyKey), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
