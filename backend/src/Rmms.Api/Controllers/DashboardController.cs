using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Dashboard;
using Rmms.Domain.Enums;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin/BUH/Leader dashboard (M15 Phase 1A basic, AC-27). Top-of-page KPI summary;
/// the per-member list lives in <c>/team-monitoring/today</c>.
/// </summary>
[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public DashboardController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>KPI summary scoped to the caller (Admin/BUH = all; Leader = managed PGs).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        if (role == UserRole.Pg) return Forbid(); // dashboard is for Leader/BUH/Admin
        var result = await _mediator.Send(new GetDashboardSummaryQuery(userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
