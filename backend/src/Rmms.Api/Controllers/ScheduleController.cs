using System.Globalization;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Scheduling;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Scheduling;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Api.Controllers;

/// <summary>
/// Work schedule management (M07). Self-service for the caller (PG/Leader) plus Leader/Admin
/// review (view + approve/reject), with Leader-scoping in the handlers (BR-405/BR-406).
/// </summary>
[ApiController]
[Route("api/v1/schedule")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class ScheduleController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ScheduleController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    private bool CallerIsAdmin => _currentUser.Role == UserRole.Admin;

    // ----- Self-service (caller's own schedule) -----

    /// <summary>The caller's schedules in a date range.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetMyScheduleQuery(userId, from, to), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Register a schedule for one or more days (BR-301).</summary>
    [HttpPost("me")]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();

        var days = new List<ScheduleDayRequest>(request.Days.Count);
        foreach (var day in request.Days)
        {
            if (!TryMapShifts(day.Shifts, out var shifts, out var error))
            {
                return ResultMapping.Failure(error, HttpContext.TraceIdentifier);
            }
            days.Add(new ScheduleDayRequest(day.Date, shifts));
        }

        var result = await _mediator.Send(new CreateScheduleCommand(userId, days), ct);
        return result.IsSuccess
            ? ResultMapping.Ok(new { ids = result.Value })
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Submit a draft schedule for approval (BR-307).</summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new SubmitScheduleCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Edit a schedule's shifts (BR-306/BR-308 — approved edits create a new version).</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] EditScheduleRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        if (!TryMapShifts(request.Shifts, out var shifts, out var error))
        {
            return ResultMapping.Failure(error, HttpContext.TraceIdentifier);
        }

        var result = await _mediator.Send(new EditScheduleCommand(id, userId, shifts), ct);
        return result.IsSuccess
            ? ResultMapping.Ok(new { id = result.Value })
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Withdraw a pending / edit-pending schedule.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Withdraw([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new WithdrawScheduleCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    // ----- Leader/Admin review -----

    /// <summary>A managed user's schedules (Leader-scoped in the handler).</summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrLeader)]
    public async Task<IActionResult> GetForUser(
        [FromRoute] Guid userId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } callerId) return Unauthorized();
        var result = await _mediator.Send(new GetUserScheduleQuery(callerId, CallerIsAdmin, userId, from, to), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Approve a pending / edit-pending schedule.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrLeader)]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } approverId) return Unauthorized();
        var result = await _mediator.Send(new ApproveScheduleCommand(id, approverId, CallerIsAdmin), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Reject a pending / edit-pending schedule (reason required).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrLeader)]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectScheduleRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } approverId) return Unauthorized();
        var result = await _mediator.Send(new RejectScheduleCommand(id, approverId, CallerIsAdmin, request.Reason), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    // ----- helpers -----

    private static bool TryMapShifts(
        IEnumerable<ShiftRequestDto> source,
        out List<ScheduleShiftRequest> shifts,
        out Error error)
    {
        shifts = new List<ScheduleShiftRequest>();
        error = default!;
        foreach (var s in source)
        {
            if (!TimeOnly.TryParseExact(s.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !TimeOnly.TryParseExact(s.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            {
                error = Error.Validation("INVALID_VALUE", "Giờ ca phải đúng định dạng HH:mm.");
                shifts = new List<ScheduleShiftRequest>();
                return false;
            }
            shifts.Add(new ScheduleShiftRequest(s.StoreId, start, end));
        }
        return true;
    }
}
