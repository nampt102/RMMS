using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.TeamMonitoring;
using Rmms.Domain.Enums;

namespace Rmms.Api.Controllers;

/// <summary>
/// Team monitoring (M12, AC-26/27): today's work status for the team in the caller's scope.
/// Admin/BUH see everyone; a Leader sees their managed PGs.
/// </summary>
[ApiController]
[Route("api/v1/team-monitoring")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class TeamMonitoringController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public TeamMonitoringController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Today's per-member status + summary counts.</summary>
    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        if (role == UserRole.Pg) return Forbid(); // monitoring is for Leader/BUH/Admin
        var result = await _mediator.Send(new GetTeamTodayQuery(userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
