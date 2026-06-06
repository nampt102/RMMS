using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Face;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.FaceRecognition;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin face management (M06): enroll on a user's behalf, force re-enroll (clears so the user
/// must enroll again), and remove a user's face template. Admin-only.
/// </summary>
[ApiController]
[Route("api/v1/admin/face")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminFaceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminFaceController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Enroll a user's face on their behalf (multipart, 1..5 photos) — Admin upload.</summary>
    [HttpPost("enroll/{userId:guid}")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Enroll([FromRoute] Guid userId, [FromForm] EnrollFaceForm form, CancellationToken ct)
    {
        var photos = new List<PhotoUpload>();
        foreach (var file in form.Photos)
        {
            if (file is null || file.Length == 0) continue;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            photos.Add(new PhotoUpload(file.FileName, file.ContentType, ms.ToArray()));
        }

        var result = await _mediator.Send(new EnrollFaceCommand(userId, photos), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Trigger a user to re-enroll (clears their current enrollment).</summary>
    [HttpPost("re-enroll/{userId:guid}")]
    public async Task<IActionResult> ReEnroll([FromRoute] Guid userId, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } adminId) return Unauthorized();
        var result = await _mediator.Send(new AdminRemoveFaceCommand(userId, adminId, ReEnroll: true), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Remove a user's face template entirely.</summary>
    [HttpDelete("template/{userId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid userId, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } adminId) return Unauthorized();
        var result = await _mediator.Send(new AdminRemoveFaceCommand(userId, adminId, ReEnroll: false), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
