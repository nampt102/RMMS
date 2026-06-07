using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Audit;

namespace Rmms.Api.Controllers;

/// <summary>Admin audit-log explorer (M16, AC-35) — read-only over the append-only audit table.</summary>
[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminAuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? action,
        [FromQuery] string? targetEntity,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(action, targetEntity, actorUserId, from, to, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
