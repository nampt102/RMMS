using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Attendance;
using Rmms.Application.Attendance;
using Rmms.Application.Common.Interfaces;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin attendance views (M05): the filterable list / anomaly review queue and the
/// approve/reject review action (BR-208/BR-209). Admin-only.
/// </summary>
[ApiController]
[Route("api/v1/admin/attendance")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminAttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminAttendanceController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Filterable attendance list (user / store / status / date range).</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? userId, [FromQuery] Guid? storeId, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new AdminListAttendanceQuery(userId, storeId, status, from, to, page, pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Approve or reject a pending-review attendance (reject reason required).</summary>
    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review([FromRoute] Guid id, [FromBody] ReviewAttendanceRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } reviewerId) return Unauthorized();
        var result = await _mediator.Send(new ReviewAttendanceCommand(id, reviewerId, request.Approve, request.Note), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
