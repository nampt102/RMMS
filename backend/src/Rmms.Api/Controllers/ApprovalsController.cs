using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Approvals;
using Rmms.Application.Approvals;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.Api.Controllers;

/// <summary>
/// Approval workflow (M09): the approver's pending queue + detail + approve/reject
/// for authenticated Leader/BUH (AC-17), plus the public BUH email-link endpoints
/// (BR-407 / AC-18) which require no login.
/// </summary>
[ApiController]
[Route("api/v1/approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ApprovalsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Pending approvals routed to the current user.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetPendingApprovalsQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Approval detail (requester / approver / admin only).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    public async Task<IActionResult> Detail([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetApprovalDetailQuery(id, userId, _currentUser.Role == UserRole.Admin), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Approve a pending request.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new ApproveApprovalCommand(id, userId, ApprovalDecisionVia.App), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Reject a pending request (reason required).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectApprovalRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new RejectApprovalCommand(id, userId, request.Reason, ApprovalDecisionVia.App), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Public: preview an email link without consuming it (landing page render).</summary>
    [HttpGet("email-action")]
    [AllowAnonymous]
    public async Task<IActionResult> EmailActionPreview([FromQuery] string token, CancellationToken ct)
    {
        var result = await _mediator.Send(new EmailActionPreviewQuery(token ?? string.Empty), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Public: submit a BUH decision via the signed link (one-time use, logs IP/UA).</summary>
    [HttpPost("email-action/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> EmailActionConfirm([FromBody] EmailActionConfirmRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var result = await _mediator.Send(
            new EmailActionConfirmCommand(request.Token, request.Action, request.Reason, ip, string.IsNullOrWhiteSpace(ua) ? null : ua), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
