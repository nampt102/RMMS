using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Organization;
using Rmms.Application.Organization.Assignments;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin assignment management (M03): PG↔Leader (1:1 active), User↔Store (1:N), User↔Category.
/// Leader-scoped management is enabled in a later slice once role-scoping is wired.
/// </summary>
[ApiController]
[Route("api/v1/admin/assignments")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AssignmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssignmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>All active assignments for one user (leader + stores + categories).</summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetForUser([FromRoute] Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserAssignmentsQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("pg-leader")]
    public async Task<IActionResult> AssignPgLeader([FromBody] AssignPgLeaderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignPgLeaderCommand(request.PgUserId, request.LeaderUserId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("user-store")]
    public async Task<IActionResult> AssignUserStore([FromBody] AssignUserStoreRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignUserStoreCommand(request.UserId, request.StoreId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("user-store")]
    public async Task<IActionResult> UnassignUserStore([FromQuery] Guid userId, [FromQuery] Guid storeId, CancellationToken ct)
    {
        var result = await _mediator.Send(new UnassignUserStoreCommand(userId, storeId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("user-category")]
    public async Task<IActionResult> AssignUserCategory([FromBody] AssignUserCategoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignUserCategoryCommand(request.UserId, request.CategoryId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("user-category")]
    public async Task<IActionResult> UnassignUserCategory([FromQuery] Guid userId, [FromQuery] Guid categoryId, CancellationToken ct)
    {
        var result = await _mediator.Send(new UnassignUserCategoryCommand(userId, categoryId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
