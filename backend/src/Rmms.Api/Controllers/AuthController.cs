using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Auth;
using Rmms.Application.Auth.ForgotPassword;
using Rmms.Application.Auth.Login;
using Rmms.Application.Auth.Logout;
using Rmms.Application.Auth.Refresh;
using Rmms.Application.Auth.Register;
using Rmms.Application.Auth.ResetPassword;
using Rmms.Application.Auth.VerifyEmail;

namespace Rmms.Api.Controllers;

/// <summary>
/// Auth endpoints per <c>knowledge-base/05-api-conventions.md</c> §Authentication
/// and M01 spec.
///
/// Sprint 01 endpoints:
///   Day 2 ✓ register, verify-email
///   Day 3 ✓ login, refresh, logout
///   Day 4 ⏭ forgot-password, reset-password
///   Day 5 ⏭ /me
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    // ─────────────────────────────────────────────────────────────────────────
    // Day 2 — register + verify-email
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>PG self-registration (BR-101). Creates user pending verification + emails link.</summary>
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

    /// <summary>Confirm registration email. Idempotent.</summary>
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

    // ─────────────────────────────────────────────────────────────────────────
    // Day 3 — login + refresh + logout
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Login with email + password + device fingerprint. Returns JWT access + refresh tokens.
    /// </summary>
    /// <remarks>
    /// **Device check (BR-105):** every PG can have at most 1 active device.
    /// First device → auto-active. Different device while another is active → 403 DEVICE_NOT_AUTHORIZED,
    /// pending_approval row created for Leader/Admin approval (Sprint 02).
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(
            Email: request.Email,
            Password: request.Password,
            Device: new LoginDeviceInfo(
                DeviceId: request.Device.DeviceId,
                DeviceName: request.Device.DeviceName,
                Os: request.Device.Os,
                OsVersion: request.Device.OsVersion ?? string.Empty,
                AppVersion: request.Device.AppVersion ?? string.Empty,
                FcmToken: request.Device.FcmToken));

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Rotate access + refresh tokens. Old refresh is revoked atomically.
    /// Reuse of an already-revoked refresh token revokes ALL active tokens for the user (security).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Revoke a refresh token (logout from one device). Idempotent.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var command = new LogoutCommand(request.RefreshToken);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Day 4 — forgot-password + reset-password
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Request a password-reset email. Always returns 204 to avoid leaking
    /// whether the email is registered (timing-attack mitigation).
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ForgotPasswordCommand(request.Email), ct);
        return NoContent();
    }

    /// <summary>
    /// Apply a new password using the single-use reset token from email.
    /// On success, ALL active refresh tokens for the user are revoked (force re-login everywhere).
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword), ct);
        return result.IsSuccess
            ? NoContent()
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
