using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Organization;
using Rmms.Application.Organization.Products;

namespace Rmms.Api.Controllers;

/// <summary>Admin CRUD for the Product Master (M04, AC-25). Source data for Form Engine selectors.</summary>
[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(search, categoryId, ActiveOnly: false, page, pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProductCommand(request.Sku, request.Name, request.Brand, request.CategoryId, request.Attributes), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProductCommand(id, request.Name, request.Brand, request.CategoryId, request.Attributes), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeProductStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeProductStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
