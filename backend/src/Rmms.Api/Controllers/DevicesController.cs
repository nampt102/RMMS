using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Devices;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Devices.ApproveDevice;
using Rmms.Application.Devices.GetMyDevice;
using Rmms.Application.Devices.GetPendingDevices;
using Rmms.Application.Devices.RejectDevice;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Api.Controllers;

/// <summary>
/// Device management (M02 / BR-105 / BR-106).
///
/// Sprint 02 slice: approval endpoints are <see cref="AuthorizationPolicies.AdminOnly"/>.
/// Leader-scoped approval (a Leader approving only their assigned PGs) is enabled once
/// M03 <c>user_leader_assignments</c> ships — switch the policy + add the assignment filter then.
/// </summary>
[ApiController]
[Route("api/v1/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public DevicesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>The caller's active device + any pending device-change request.</summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetMyDeviceQuery(userId), ct);
        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Device-change requests awaiting approval (Admin).</summary>
    [HttpGet("pending")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPendingDevicesQuery(), ct);
        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Approve a pending device-change request (BR-106).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } approverId)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ApproveDeviceCommand(id, approverId), ct);
        return result.IsSuccess
            ? NoContent()
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Reject a pending device-change request with a reason (BR-106).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectDeviceRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } approverId)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RejectDeviceCommand(id, approverId, request.Reason), ct);
        return result.IsSuccess
            ? NoContent()
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
