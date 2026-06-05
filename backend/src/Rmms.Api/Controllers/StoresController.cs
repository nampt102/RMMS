using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Organization;
using Rmms.Application.Organization.Stores;

namespace Rmms.Api.Controllers;

/// <summary>Admin CRUD for stores (M03). Store = retail location with GPS coords.</summary>
[ApiController]
[Route("api/v1/admin/stores")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class StoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoresController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? areaId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStoresQuery(page, pageSize, areaId, status, search), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateStoreCommand(request.Code, request.Name, request.Address, request.Latitude, request.Longitude, request.AreaId), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateStoreRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateStoreCommand(id, request.Name, request.Address, request.Latitude, request.Longitude, request.AreaId), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeStoreStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeStoreStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteStoreCommand(id), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
