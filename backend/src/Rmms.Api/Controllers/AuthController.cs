using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Auth;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;
using Rmms.Application.Auth.ForgotPassword;
using Rmms.Application.Auth.Login;
using Rmms.Application.Auth.Me;
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
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoginRateLimiter _loginRateLimiter;
    private readonly ICurrentUser _currentUser;

    public AuthController(IMediator mediator, ILoginRateLimiter loginRateLimiter, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _loginRateLimiter = loginRateLimiter;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Resolve the caller IP, honoring the first <c>X-Forwarded-For</c> hop set by the
    /// Caddy reverse proxy (ADR-007), falling back to the socket remote address.
    /// </summary>
    private string ClientIp()
    {
        var fwd = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            return fwd.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Day 2 — register + verify-email
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>PG self-registration (BR-101). Creates user pending verification + emails link.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = ClientIp();

        // Rate limit per (email + IP): 5 failures / 15 min (sprint-01 Day 5).
        if (await _loginRateLimiter.IsBlockedAsync(request.Email, ip, ct))
        {
            return TooManyAttempts();
        }

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

        if (result.IsSuccess)
        {
            await _loginRateLimiter.ResetAsync(request.Email, ip, ct);
            return ResultMapping.Ok(result.Value);
        }

        // Count only credential failures toward the limit (not e.g. device/validation errors).
        if (result.Error.Code == ErrorCodes.InvalidCredentials)
        {
            await _loginRateLimiter.RegisterFailureAsync(request.Email, ip, ct);
        }

        return ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    private ObjectResult TooManyAttempts() =>
        new ObjectResult(new ErrorEnvelope(new ErrorBody(
            ErrorCodes.RateLimitExceeded,
            "Quá nhiều lần đăng nhập thất bại. Vui lòng thử lại sau 15 phút.",
            null,
            HttpContext.TraceIdentifier)))
        {
            StatusCode = StatusCodes.Status429TooManyRequests,
        };

    /// <summary>
    /// Rotate access + refresh tokens. Old refresh is revoked atomically.
    /// Reuse of an already-revoked refresh token revokes ALL active tokens for the user (security).
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    // ─────────────────────────────────────────────────────────────────────────
    // Day 5 — current user
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Return the authenticated user's profile + the device tied to the current access token.
    /// Identity comes from JWT claims, never the request body.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ResultMapping.Failure(
                Error.Unauthorized(ErrorCodes.TokenInvalid, "Bạn cần đăng nhập."),
                HttpContext.TraceIdentifier);
        }

        var result = await _mediator.Send(new GetMeQuery(userId, _currentUser.DeviceId), ct);
        return result.IsSuccess
            ? ResultMapping.Ok(result.Value)
            : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
