using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Attendance;
using Rmms.Application.Attendance;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Api.Controllers;

/// <summary>
/// Self-service attendance for PG/Leader (M05): today's shifts, check-in screen bootstrap,
/// multipart check-in / check-out, and personal history. Identity comes from the JWT.
/// No offline support in Phase 1 (BR-210).
/// </summary>
[ApiController]
[Route("api/v1/attendance")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AttendanceController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Today's expected shifts + their current attendance status.</summary>
    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetTodayQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Assigned stores + validation thresholds + today's shifts (check-in screen bootstrap).</summary>
    [HttpGet("check-in/info")]
    public async Task<IActionResult> CheckInInfo(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetCheckInInfoQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Submit a check-in (multipart). BR-201..BR-210.</summary>
    [HttpPost("check-in")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> CheckIn([FromForm] CheckInForm form, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();

        if (!InvariantFormParsing.TryParseGps(Request.Form, out var latitude, out var longitude, out var accuracyMeters))
        {
            return ResultMapping.Failure(
                Error.Validation(ErrorCodes.ValidationFailed, "Dữ liệu không hợp lệ."),
                HttpContext.TraceIdentifier);
        }

        var command = new CheckInCommand(
            userId, form.StoreId, latitude, longitude, accuracyMeters, form.FakeGpsDetected,
            await ToUploadAsync(form.Selfie, ct), await ToUploadAsync(form.StorePhoto, ct), form.Note);

        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? ResultMapping.Created(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Submit a check-out for an open attendance (multipart). BR-206.</summary>
    [HttpPost("{id:guid}/check-out")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> CheckOut([FromRoute] Guid id, [FromForm] CheckOutForm form, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();

        if (!InvariantFormParsing.TryParseGps(Request.Form, out var latitude, out var longitude, out var accuracyMeters))
        {
            return ResultMapping.Failure(
                Error.Validation(ErrorCodes.ValidationFailed, "Dữ liệu không hợp lệ."),
                HttpContext.TraceIdentifier);
        }

        var command = new CheckOutCommand(
            userId, id, latitude, longitude, accuracyMeters, form.FakeGpsDetected,
            await ToUploadAsync(form.Selfie, ct), await ToUploadAsync(form.StorePhoto, ct), form.Note);

        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>The caller's attendance history (paginated, optional date range).</summary>
    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetHistoryQuery(userId, from, to, page, pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    private static async Task<PhotoUpload?> ToUploadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return new PhotoUpload(file.FileName, file.ContentType, ms.ToArray());
    }
}
