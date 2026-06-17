using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Organization.Views;

namespace Rmms.Api.Controllers;

/// <summary>
/// Org visualization (M03): read-only aggregated views for the admin "Organization" screen —
/// the rank hierarchy (BUH → Leader → PG) and the area tree (Area → Store → assigned employees).
/// One aggregated call per view (Phase-1 scale) to keep the tree render N+1-free.
/// </summary>
[ApiController]
[Route("api/v1/admin/org")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class OrgController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrgController(IMediator mediator) => _mediator = mediator;

    /// <summary>Rank hierarchy tree: BUHs, Leaders (with their PGs), and PGs without a Leader.</summary>
    [HttpGet("hierarchy")]
    public async Task<IActionResult> Hierarchy(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrgHierarchyQuery(), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Area tree: areas (flat, with parentAreaId) each carrying stores + assigned employees.</summary>
    [HttpGet("area-tree")]
    public async Task<IActionResult> AreaTree(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrgAreaTreeQuery(), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
