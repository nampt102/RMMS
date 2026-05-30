using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Security;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Auth.ResetPassword;

/// <summary>
/// Handles <see cref="ResetPasswordCommand"/>:
///   1. Hash token → DB lookup.
///   2. Reject: not-found / used / expired → <see cref="ErrorCodes.TokenInvalid"/> / <see cref="ErrorCodes.EmailTokenUsed"/> / <see cref="ErrorCodes.EmailTokenExpired"/>.
///   3. Hash new password (BCrypt cost 12) + apply via <c>user.ChangePassword</c>.
///   4. Mark token used.
///   5. **Revoke ALL active refresh tokens for this user** (force re-login on every device).
///   6. Audit <see cref="AuditAction.UserPasswordReset"/>.
///   7. SaveChanges atomically.
/// </summary>
internal sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ResetPasswordCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IAuditLogger audit,
        IDateTimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var tokenHash = OpaqueToken.Hash(command.Token);

        var token = await _db.PasswordResetTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (token is null)
        {
            return Result.Failure(Error.Validation(ErrorCodes.TokenInvalid, "Token đặt lại mật khẩu không hợp lệ."));
        }

        if (token.UsedAt is not null)
        {
            return Result.Failure(Error.Validation(ErrorCodes.EmailTokenUsed, "Token đã được sử dụng."));
        }

        if (now >= token.ExpiresAt)
        {
            return Result.Failure(Error.Validation(ErrorCodes.EmailTokenExpired, "Token đã hết hạn. Vui lòng yêu cầu lại."));
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null)
        {
            return Result.Failure(Error.Validation(ErrorCodes.TokenInvalid, "Token không hợp lệ."));
        }

        // Apply new password
        var newHash = _hasher.Hash(command.NewPassword);
        user.ChangePassword(newHash);

        // Mark token used
        token.MarkUsed(now);

        // ----- Revoke ALL active refresh tokens (force re-login on every device) -----
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in activeTokens)
        {
            t.Revoke(now);
        }

        await _audit.RecordAsync(
            action: AuditAction.UserPasswordReset,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, revoked_refresh_count = activeTokens.Count },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
