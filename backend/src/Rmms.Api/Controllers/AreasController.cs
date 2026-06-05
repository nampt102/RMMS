using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Organization;
using Rmms.Application.Organization.Areas;

namespace Rmms.Api.Controllers;

/// <summary>Admin CRUD for areas / khu vực (M03).</summary>
[ApiController]
[Route("api/v1/admin/areas")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AreasController : ControllerBase
{
    private readonly IMediator _mediator;

    public AreasController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAreasQuery(), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAreaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAreaCommand(request.Code, request.Name, request.ParentAreaId), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAreaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAreaCommand(id, request.Name, request.ParentAreaId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAreaCommand(id), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
