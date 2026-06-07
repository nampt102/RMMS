using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.LeaveOt;

namespace Rmms.Api.Controllers;

/// <summary>Admin read view of all leave / OT requests (M08). Override is handled via /admin/approvals.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("leave-requests")]
    public async Task<IActionResult> Leave([FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllLeaveRequestsQuery(status, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("ot-requests")]
    public async Task<IActionResult> Ot([FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllOtRequestsQuery(status, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
