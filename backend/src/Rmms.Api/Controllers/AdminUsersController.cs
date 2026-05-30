using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Admin;
using Rmms.Application.Admin.Users.AdminResetPassword;
using Rmms.Application.Admin.Users.CreateAdminUser;
using Rmms.Application.Admin.Users.GetUsers;
using Rmms.Application.Admin.Users.UpdateUser;

namespace Rmms.Api.Controllers;

/// <summary>
/// Admin-only user CRUD per M01 spec.
///
/// Authorization: <c>Roles = "admin"</c> — JWT role claim mapping configured in
/// <c>Program.cs</c> (<c>RoleClaimType = "role"</c>).
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Paginated list of users with optional role / status / search filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUsersQuery(page, pageSize, role, status, search), ct);
        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Create a Leader / BUH / Admin user. PG accounts must self-register (BR-101).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var command = new CreateAdminUserCommand(
            Email: request.Email,
            FullName: request.FullName,
            Phone: request.Phone,
            Role: request.Role,
            PreferredLanguage: request.PreferredLanguage);

        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? ResultMapping.Created(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Update profile (name/phone/language) + toggle status (active/inactive).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var command = new UpdateUserCommand(
            UserId: id,
            FullName: request.FullName,
            Phone: request.Phone,
            Status: request.Status,
            PreferredLanguage: request.PreferredLanguage);

        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Force-issue a password-reset email for any user (admin override).</summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdminResetPasswordCommand(id), ct);
        return result.IsSuccess
            ? NoContent()
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
