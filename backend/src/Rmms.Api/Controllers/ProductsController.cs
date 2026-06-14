using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Organization.Products;

namespace Rmms.Api.Controllers;

/// <summary>
/// Read-only Product Master for mobile (M04, AC-25) — used by Form Engine product selectors.
/// Returns active products only, paginated + searchable. Any authenticated user.
/// </summary>
[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(search, categoryId, ActiveOnly: true, page, pageSize), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
