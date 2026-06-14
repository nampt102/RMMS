using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.VisitPlans;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.VisitPlans;

namespace Rmms.Api.Controllers;

/// <summary>
/// Leader surface for Visit Plans (M11, AC-28/30): create a plan (routes a BUH approval), edit
/// while pending, list own plans, view detail, and link a post-visit form submission per store.
/// </summary>
[ApiController]
[Route("api/v1/visit-plans")]
[Authorize(Policy = AuthorizationPolicies.LeaderOnly)]
public sealed class VisitPlansController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public VisitPlansController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public async Task<IActionResult> MyPlans(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetMyVisitPlansQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role) return Unauthorized();
        var result = await _mediator.Send(new GetVisitPlanQuery(id, userId, role), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVisitPlanRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var items = (request.Items ?? new List<VisitPlanItemRequest>())
            .Select(i => new VisitPlanItemInput(i.StoreId, i.FormId)).ToList();
        var result = await _mediator.Send(new CreateVisitPlanCommand(userId, request.VisitDate, request.Notes, items), ct);
        return result.IsSuccess ? ResultMapping.Created(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] EditVisitPlanRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var items = (request.Items ?? new List<VisitPlanItemRequest>())
            .Select(i => new VisitPlanItemInput(i.StoreId, i.FormId)).ToList();
        var result = await _mediator.Send(new EditVisitPlanCommand(id, userId, request.VisitDate, request.Notes, items), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/execute")]
    public async Task<IActionResult> Execute(
        [FromRoute] Guid id, [FromRoute] Guid itemId, [FromBody] ExecuteVisitItemRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new ExecuteVisitItemCommand(id, itemId, userId, request.FormSubmissionId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
