using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.VisitPlans;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin read-only view of all Visit Plans (M11). Decisions flow through the M09 approval
/// surface (BUH approves; Admin may override) — this controller is for oversight/reporting.
/// </summary>
[ApiController]
[Route("api/v1/admin/visit-plans")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminVisitPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminVisitPlansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] Guid? leaderUserId, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdminGetVisitPlansQuery(status, leaderUserId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
