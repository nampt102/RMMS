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

/// <summary>Leave requests (M08): create regular / emergency, own history, withdraw. Routed to the M09 approval engine.</summary>
[ApiController]
[Route("api/v1/leave-requests")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class LeaveRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public LeaveRequestsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestBody body, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(
            new CreateLeaveRequestCommand(userId, body.StartDate, body.EndDate, body.StartTime, body.EndTime, body.Reason), ct);
        return result.IsSuccess ? ResultMapping.Created(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("emergency")]
    public async Task<IActionResult> CreateEmergency([FromBody] EmergencyLeaveBody body, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new CreateEmergencyLeaveCommand(userId, body.Reason), ct);
        return result.IsSuccess ? ResultMapping.Created(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetMyLeaveRequestsQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Withdraw([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new WithdrawLeaveRequestCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
