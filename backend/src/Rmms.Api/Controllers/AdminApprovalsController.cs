using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Approvals;
using Rmms.Application.Approvals;
using Rmms.Application.Common.Interfaces;

namespace Rmms.Api.Controllers;

/// <summary>Admin override of approvals (M09, BR-408 / AC-19) — reason required, audited.</summary>
[ApiController]
[Route("api/v1/admin/approvals")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminApprovalsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("{id:guid}/override")]
    public async Task<IActionResult> Override([FromRoute] Guid id, [FromBody] OverrideApprovalRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } adminId) return Unauthorized();
        var result = await _mediator.Send(new OverrideApprovalCommand(id, adminId, request.Reason), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
