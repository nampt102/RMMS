using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Options;
using Rmms.Domain.Auth;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Auth.Refresh;

/// <summary>
/// Handles <see cref="RefreshTokenCommand"/>:
///   1. SHA-256 hash incoming plaintext → unique-index lookup.
///   2. If not found → <see cref="ErrorCodes.RefreshTokenRevoked"/> (don't leak whether token existed).
///   3. If token already revoked → REUSE DETECTED:
///        - Revoke ALL active refresh tokens for that user.
///        - Audit <see cref="AuditAction.AuthRefreshReused"/> with severity.
///        - Return <see cref="ErrorCodes.RefreshTokenReused"/>; client must re-login.
///   4. If expired → <see cref="ErrorCodes.RefreshTokenRevoked"/>.
///   5. Issue new access + refresh; link old → new via <c>ReplacedByTokenId</c>; revoke old.
///   6. Audit <see cref="AuditAction.AuthRefreshRotated"/>.
///   7. SaveChanges atomically.
///
/// User must still be Active. If status changed to Inactive between login and refresh,
/// return <see cref="ErrorCodes.AccountInactive"/>.
/// </summary>
internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenGenerator _refreshTokenGen;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;
    private readonly TimeSpan _refreshLifetime;

    public RefreshTokenCommandHandler(
        IAppDbContext db,
        IJwtTokenService jwt,
        IRefreshTokenGenerator refreshTokenGen,
        IAuditLogger audit,
        IDateTimeProvider clock,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwt = jwt;
        _refreshTokenGen = refreshTokenGen;
        _audit = audit;
        _clock = clock;
        _refreshLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenDays);
    }

    public async ValueTask<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var tokenHash = _refreshTokenGen.Hash(command.RefreshToken);

        var token = await _db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (token is null)
        {
            await _audit.RecordAsync(
                action: AuditAction.AuthLoginFailed,
                targetEntity: "refresh_token",
                targetId: null,
                metadata: new { reason = "refresh_token_unknown" },
                ct: ct);
            await _db.SaveChangesAsync(ct);
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized(ErrorCodes.RefreshTokenRevoked, "Token không hợp lệ hoặc đã bị thu hồi."));
        }

        // ----- Reuse detection -----
        if (token.RevokedAt is not null)
        {
            // The token has already been used (revoked during rotation). Re-presenting it means
            // either (a) malicious replay, or (b) a sloppy client retried after first rotation reply lost.
            // Either way, the safe action is to nuke all active sessions for the user.
            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var t in activeTokens)
            {
                t.Revoke(now);
            }

            await _audit.RecordAsync(
                action: AuditAction.AuthRefreshReused,
                targetEntity: "user",
                targetId: token.UserId,
                metadata: new
                {
                    reused_token_id = token.Id,
                    revoked_count = activeTokens.Count,
                    severity = "high",
                },
                ct: ct);

            await _db.SaveChangesAsync(ct);

            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized(ErrorCodes.RefreshTokenReused,
                    "Phát hiện token đã bị sử dụng lại. Tất cả phiên đăng nhập đã được đăng xuất. Vui lòng đăng nhập lại."));
        }

        if (now >= token.ExpiresAt)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized(ErrorCodes.RefreshTokenRevoked, "Token đã hết hạn. Vui lòng đăng nhập lại."));
        }

        // ----- Look up user — must still be active -----
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized(ErrorCodes.RefreshTokenRevoked, "Token không hợp lệ."));
        }
        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Forbidden(ErrorCodes.AccountInactive, "Tài khoản không hoạt động."));
        }

        // ----- Issue new tokens -----
        var newAccess = _jwt.IssueAccess(user.Id, user.Email, user.Role, token.DeviceId, now);
        var newRefresh = _refreshTokenGen.Generate();
        var newRow = RefreshToken.Issue(user.Id, token.DeviceId, newRefresh.Hash, now, _refreshLifetime);
        _db.RefreshTokens.Add(newRow);

        // Link old → new + revoke old.
        token.MarkRotatedBy(newRow.Id, now);

        await _audit.RecordAsync(
            action: AuditAction.AuthRefreshRotated,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { old_token_id = token.Id, new_token_id = newRow.Id, device_id = token.DeviceId },
            ct: ct);

        await _db.SaveChangesAsync(ct);

        return new RefreshTokenResponse(
            AccessToken: newAccess.Token,
            AccessTokenExpiresAt: newAccess.ExpiresAt,
            RefreshToken: newRefresh.Plaintext,
            RefreshTokenExpiresAt: newRow.ExpiresAt);
    }
}
