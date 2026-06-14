using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Reports;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin reports (M15, AC-27): attendance + anomaly reports with date-range/store/user filters,
/// a daily attendance trend for the dashboard chart, and Excel (.xlsx) export of each report.
///
/// Phase 1: Admin-scoped (all data). BUH area-scoping / Leader own-PGs scoping is a Phase-2
/// refinement (the live dashboard summary already scopes per role separately).
/// </summary>
[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] Guid? storeId, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAttendanceReportQuery(from, to, storeId, userId, AnomaliesOnly: false), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("anomalies")]
    public async Task<IActionResult> Anomalies(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] Guid? storeId, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAttendanceReportQuery(from, to, storeId, userId, AnomaliesOnly: true), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("attendance-trend")]
    public async Task<IActionResult> AttendanceTrend([FromQuery] int days = 14, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAttendanceTrendQuery(days), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("attendance/export")]
    public Task<IActionResult> ExportAttendance(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] Guid? storeId, [FromQuery] Guid? userId, CancellationToken ct)
        => ExportAsync(from, to, storeId, userId, anomaliesOnly: false, ct);

    [HttpGet("anomalies/export")]
    public Task<IActionResult> ExportAnomalies(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] Guid? storeId, [FromQuery] Guid? userId, CancellationToken ct)
        => ExportAsync(from, to, storeId, userId, anomaliesOnly: true, ct);

    private async Task<IActionResult> ExportAsync(
        DateOnly from, DateOnly to, Guid? storeId, Guid? userId, bool anomaliesOnly, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportAttendanceReportCommand(from, to, storeId, userId, anomaliesOnly), ct);
        if (result.IsFailure) return ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }
}
