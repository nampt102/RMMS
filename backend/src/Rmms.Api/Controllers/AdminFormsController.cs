using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Forms;
using Rmms.Application.Forms;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin Form Builder CRUD + publish (M10, AC-20/21). Editing a published form creates a new
/// version on publish (BR-505); old versions stay immutable. Mobile fill endpoints land later.
/// </summary>
[ApiController]
[Route("api/v1/admin/forms")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminFormsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminFormsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFormsQuery(), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFormQuery(id), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> Versions([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFormVersionsQuery(id), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateFormCommand(
            request.Code, request.NameVi, request.NameEn, request.DescriptionVi, request.DescriptionEn,
            request.FormType, request.Schema), ct);
        return result.IsSuccess ? ResultMapping.Created(new { id = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateFormRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateFormDraftCommand(
            id, request.NameVi, request.NameEn, request.DescriptionVi, request.DescriptionEn, request.Schema), ct);
        return result.IsSuccess ? NoContent() : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PublishFormCommand(id), ct);
        return result.IsSuccess ? ResultMapping.Ok(new { version = result.Value }) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
