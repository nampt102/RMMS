using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.LeaveOt;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.LeaveOt;

namespace Rmms.Api.Controllers;

/// <summary>OT requests (M08): create + own history. Routed to the M09 approval engine.</summary>
[ApiController]
[Route("api/v1/ot-requests")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class OtRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OtRequestsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOtRequestBody body, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(
            new CreateOtRequestCommand(userId, body.OtDate, body.StartTime, body.EndTime, body.Reason), ct);
        return result.IsSuccess ? ResultMapping.Created(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetMyOtRequestsQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
