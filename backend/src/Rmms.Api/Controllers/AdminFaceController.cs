using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.FaceRecognition;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin face management (M06): force re-enroll (clears so the user must enroll again) and
/// remove a user's face template. Admin-only.
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
