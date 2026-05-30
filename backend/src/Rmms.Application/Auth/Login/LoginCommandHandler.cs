using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Auth;
using Rmms.Domain.Common;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;
using Rmms.Shared.Errors;

namespace Rmms.Application.Auth.Login;

/// <summary>
/// Handles <see cref="LoginCommand"/>:
///   1. Lookup user by normalized email. Always emit a <c>login_history</c> row.
///   2. Reject inactive / unverified / wrong-password — return <see cref="ErrorCodes.InvalidCredentials"/>
///      (don't leak which check failed — same code for "no user" and "wrong password").
///   3. Device check (BR-105):
///        - User has no active device → register new as Active (auto-approve, first install).
///        - User has active device with matching <c>device_id</c> → reuse, touch <c>last_used_at</c>.
///        - User has active device with DIFFERENT <c>device_id</c> → create pending_approval row
///          (or reuse existing pending for this device) and return 403 DEVICE_NOT_AUTHORIZED.
///   4. Issue access + refresh tokens. Persist refresh hash.
///   5. Record <c>login_history</c> success + audit <see cref="AuditAction.AuthLoginSuccess"/>.
///   6. SaveChanges atomically (login_history + audit + refresh_token + user.last_login_at touch).
/// </summary>
internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenGenerator _refreshTokenGen;
    private readonly IAuditLogger _audit;
    private readonly IClientContext _clientContext;
    private readonly IDateTimeProvider _clock;
    private readonly TimeSpan _refreshLifetime;

    public LoginCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenGenerator refreshTokenGen,
        IAuditLogger audit,
        IClientContext clientContext,
        IDateTimeProvider clock,
        Microsoft.Extensions.Options.IOptions<Rmms.Application.Common.Options.JwtOptions> jwtOptions)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _refreshTokenGen = refreshTokenGen;
        _audit = audit;
        _clientContext = clientContext;
        _clock = clock;
        _refreshLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenDays);
    }

    public async ValueTask<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        // ----- 1. Lookup user (soft-delete query filter excludes deleted users) -----
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null)
        {
            // Don't leak — emit minimal login_history row (no user_id) and return generic error.
            // Note: login_history.UserId is non-nullable in spec; skip the row in this case to keep schema clean.
            await _audit.RecordAsync(
                action: AuditAction.AuthLoginFailed,
                targetEntity: "auth",
                targetId: null,
                metadata: new { email = normalizedEmail, reason = "user_not_found" },
                ct: ct);

            await _db.SaveChangesAsync(ct);

            return Result.Failure<LoginResponse>(
                Error.Unauthorized(ErrorCodes.InvalidCredentials, "Email hoặc mật khẩu không đúng."));
        }

        // ----- 2. Status gates -----
        if (user.Status == UserStatus.PendingEmailVerify)
        {
            return await FailLoginAsync(user, deviceId: null, reason: "email_not_verified",
                code: ErrorCodes.EmailNotVerified,
                message: "Vui lòng xác minh email trước khi đăng nhập.", ct);
        }

        if (user.Status == UserStatus.Inactive)
        {
            return await FailLoginAsync(user, deviceId: null, reason: "account_inactive",
                code: ErrorCodes.AccountInactive,
                message: "Tài khoản đã bị vô hiệu hoá. Liên hệ Admin để được hỗ trợ.", ct);
        }

        // ----- 3. Password check -----
        if (!_hasher.Verify(command.Password, user.PasswordHash))
        {
            return await FailLoginAsync(user, deviceId: null, reason: "invalid_credentials",
                code: ErrorCodes.InvalidCredentials,
                message: "Email hoặc mật khẩu không đúng.", ct);
        }

        // ----- 4. Device check (BR-105) -----
        var deviceResult = await ResolveDeviceAsync(user, command.Device, now, ct);
        if (deviceResult.IsFailure)
        {
            await _db.SaveChangesAsync(ct); // persist any login_history + audit + pending device row
            return Result.Failure<LoginResponse>(deviceResult.Error);
        }

        var device = deviceResult.Value;

        // ----- 5. Issue tokens -----
        var accessToken = _jwt.IssueAccess(user.Id, user.Email, user.Role, device.Id, now);
        var refreshGen = _refreshTokenGen.Generate();
        var refreshToken = RefreshToken.Issue(user.Id, device.Id, refreshGen.Hash, now, _refreshLifetime);
        _db.RefreshTokens.Add(refreshToken);

        // ----- 6. User touch -----
        user.RecordLogin(now);

        // ----- 7. Login history + audit -----
        _db.LoginHistory.Add(LoginHistory.RecordSuccess(
            userId: user.Id,
            deviceId: device.Id,
            ip: _clientContext.IpAddress,
            userAgent: _clientContext.UserAgent,
            at: now));

        await _audit.RecordAsync(
            action: AuditAction.AuthLoginSuccess,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, device_id = device.DeviceId, device_status = device.Status.ToSnakeCase() },
            ct: ct);

        await _db.SaveChangesAsync(ct);

        return new LoginResponse(
            AccessToken: accessToken.Token,
            AccessTokenExpiresAt: accessToken.ExpiresAt,
            RefreshToken: refreshGen.Plaintext,
            RefreshTokenExpiresAt: refreshToken.ExpiresAt,
            User: new LoginUserInfo(
                UserId: user.Id,
                Email: user.Email,
                FullName: user.FullName,
                Role: user.Role.ToSnakeCase(),
                Status: user.Status.ToSnakeCase(),
                PreferredLanguage: user.PreferredLanguage));
    }

    /// <summary>
    /// BR-105 / BR-106 device resolution. Either returns the device to use, OR
    /// returns 403 DEVICE_NOT_AUTHORIZED with a pending_approval row already created
    /// (so Sprint 02 Leader/Admin UI can approve it).
    /// </summary>
    private async Task<Result<UserDevice>> ResolveDeviceAsync(User user, LoginDeviceInfo info, DateTimeOffset now, CancellationToken ct)
    {
        // Find any device for this user with this fingerprint.
        var existing = await _db.UserDevices
            .SingleOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == info.DeviceId, ct);

        var activeDevice = await _db.UserDevices
            .SingleOrDefaultAsync(d => d.UserId == user.Id && d.Status == DeviceStatus.Active, ct);

        // Case A: existing device for this fingerprint AND it's Active → happy path.
        if (existing is not null && existing.Status == DeviceStatus.Active)
        {
            existing.Touch(now);
            existing.UpdateFcmToken(info.FcmToken);
            return existing;
        }

        // Case B: no active device for user → register this fingerprint as Active (first install).
        if (activeDevice is null && existing is null)
        {
            var first = UserDevice.RegisterFirstActive(
                userId: user.Id,
                deviceId: info.DeviceId,
                deviceName: info.DeviceName,
                os: info.Os,
                osVersion: info.OsVersion,
                appVersion: info.AppVersion,
                fcmToken: info.FcmToken,
                at: now);
            _db.UserDevices.Add(first);

            await _audit.RecordAsync(
                action: AuditAction.DeviceRegistered,
                targetEntity: "user_device",
                targetId: first.Id,
                metadata: new { user.Email, device_id = first.DeviceId, status = "active" },
                ct: ct);

            return first;
        }

        // Case C: there's an active device for the user, but THIS fingerprint is different (or pending/rejected).
        // → create or reuse pending_approval row + return DEVICE_NOT_AUTHORIZED.
        if (existing is null)
        {
            var pending = UserDevice.RegisterPendingApproval(
                userId: user.Id,
                deviceId: info.DeviceId,
                deviceName: info.DeviceName,
                os: info.Os,
                osVersion: info.OsVersion,
                appVersion: info.AppVersion,
                fcmToken: info.FcmToken);
            _db.UserDevices.Add(pending);

            await _audit.RecordAsync(
                action: AuditAction.DeviceChangeRequested,
                targetEntity: "user_device",
                targetId: pending.Id,
                metadata: new { user.Email, new_device_id = pending.DeviceId, current_device_id = activeDevice?.DeviceId },
                ct: ct);

            _db.LoginHistory.Add(LoginHistory.RecordFailure(
                userId: user.Id, deviceId: pending.Id,
                ip: _clientContext.IpAddress, userAgent: _clientContext.UserAgent,
                failureReason: "device_not_authorized", at: now));
        }
        else if (existing.Status == DeviceStatus.PendingApproval)
        {
            // already pending — still blocked
            _db.LoginHistory.Add(LoginHistory.RecordFailure(
                userId: user.Id, deviceId: existing.Id,
                ip: _clientContext.IpAddress, userAgent: _clientContext.UserAgent,
                failureReason: "device_not_authorized", at: now));
        }
        else if (existing.Status == DeviceStatus.Rejected || existing.Status == DeviceStatus.Replaced)
        {
            // Rejected / Replaced — never let them in.
            _db.LoginHistory.Add(LoginHistory.RecordFailure(
                userId: user.Id, deviceId: existing.Id,
                ip: _clientContext.IpAddress, userAgent: _clientContext.UserAgent,
                failureReason: "device_not_authorized", at: now));
        }

        return Result.Failure<UserDevice>(
            Error.Forbidden(ErrorCodes.DeviceNotAuthorized,
                "Thiết bị này chưa được phê duyệt. Vui lòng liên hệ Leader hoặc Admin để duyệt."));
    }

    private async Task<Result<LoginResponse>> FailLoginAsync(User user, Guid? deviceId, string reason, string code, string message, CancellationToken ct)
    {
        _db.LoginHistory.Add(LoginHistory.RecordFailure(
            userId: user.Id,
            deviceId: deviceId,
            ip: _clientContext.IpAddress,
            userAgent: _clientContext.UserAgent,
            failureReason: reason,
            at: _clock.UtcNow));

        await _audit.RecordAsync(
            action: AuditAction.AuthLoginFailed,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, reason },
            ct: ct);

        await _db.SaveChangesAsync(ct);

        var error = code == ErrorCodes.AccountInactive
            ? Error.Forbidden(code, message)
            : Error.Unauthorized(code, message);
        return Result.Failure<LoginResponse>(error);
    }
}
