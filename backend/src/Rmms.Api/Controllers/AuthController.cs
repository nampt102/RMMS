using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Auth;
using Rmms.Application.Auth.Register;
using Rmms.Application.Auth.VerifyEmail;

namespace Rmms.Api.Controllers;

/// <summary>
/// Auth endpoints per <c>knowledge-base/05-api-conventions.md</c> §Authentication
/// and M01 spec.
///
/// Sprint 01 Day 2: register + verify-email.
/// Sprint 01 Day 3: login + refresh + logout.
/// Sprint 01 Day 4: forgot-password + reset-password.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// PG self-registration. Creates user in <c>pending_email_verify</c> status,
    /// emails a verification link valid for 24 hours (single-use).
    /// </summary>
    /// <remarks>
    /// **BR-101**: PG accounts are always email-registered (not Admin-provisioned).
    /// Admin-created accounts (Leader/BUH/Admin) use <c>POST /admin/users</c> instead.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(
            Email: request.Email,
            Password: request.Password,
            FullName: request.FullName,
            Phone: request.Phone,
            PreferredLanguage: request.PreferredLanguage);

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? ResultMapping.Created(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Confirm a registration email by exchanging the token from the email URL.
    /// Idempotent: re-verifying an already-active user returns 200 (no error).
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var command = new VerifyEmailCommand(request.Token);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
