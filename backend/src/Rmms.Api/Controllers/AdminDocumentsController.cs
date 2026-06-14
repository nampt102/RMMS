using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Documents;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Documents;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin Document Center (M13, AC-31/32): upload (multipart) to public/private folders, assign to
/// role/user (payslip = single user), list, and delete. Binaries live in object storage under
/// random keys; downloads go through the signed-URL endpoint on <see cref="DocumentsController"/>.
/// </summary>
[ApiController]
[Route("api/v1/admin/documents")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminDocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminDocumentsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? folderType, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdminGetDocumentsQuery(folderType), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)] // 50 MB ceiling for document uploads
    public async Task<IActionResult> Upload(
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string folderType,
        IFormFile file,
        CancellationToken ct)
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
        var result = await _mediator.Send(new UploadDocumentCommand(
            name, description, folderType,
            new PhotoUpload(file.FileName, file.ContentType, ms.ToArray()), userId), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/assignments")]
    public async Task<IActionResult> Assign([FromRoute] Guid id, [FromBody] AssignDocumentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignDocumentCommand(id, request.Role, request.UserId), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteDocumentCommand(id), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
