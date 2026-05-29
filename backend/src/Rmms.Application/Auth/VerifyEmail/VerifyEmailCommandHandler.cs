using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Security;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Auth.VerifyEmail;

/// <summary>
/// Handles <see cref="VerifyEmailCommand"/>:
///   1. Hash the incoming plaintext token → look up by hash.
///   2. Reject if token unknown / expired / already used.
///   3. Look up <c>User</c> by token's <c>UserId</c>.
///   4. Call <c>user.VerifyEmail(now)</c> — domain transitions status to Active.
///   5. Mark token used (<c>UsedAt = now</c>).
///   6. Emit <see cref="AuditAction.UserEmailVerified"/> audit.
///   7. SaveChanges atomically.
/// </summary>
internal sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public VerifyEmailCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<VerifyEmailResponse>> Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var tokenHash = OpaqueToken.Hash(command.Token);
        var now = _clock.UtcNow;

        // ----- 1. Look up token by hash (covered by unique index) -----
        var token = await _db.EmailVerificationTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (token is null)
        {
            return Result.Failure<VerifyEmailResponse>(
                Error.Validation(ErrorCodes.TokenInvalid, "Token xác minh email không hợp lệ."));
        }

        if (token.UsedAt is not null)
        {
            return Result.Failure<VerifyEmailResponse>(
                Error.Validation(ErrorCodes.EmailTokenUsed, "Token đã được sử dụng."));
        }

        if (now >= token.ExpiresAt)
        {
            return Result.Failure<VerifyEmailResponse>(
                Error.Validation(ErrorCodes.EmailTokenExpired, "Token đã hết hạn. Vui lòng yêu cầu link xác minh mới."));
        }

        // ----- 2. Look up user (must exist + not soft-deleted; query filter handles deletion) -----
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null)
        {
            // Token references a vanished user — treat as invalid (don't leak existence).
            return Result.Failure<VerifyEmailResponse>(
                Error.Validation(ErrorCodes.TokenInvalid, "Token xác minh email không hợp lệ."));
        }

        // ----- 3. Domain transition + mark token used -----
        if (user.Status == UserStatus.PendingEmailVerify)
        {
            user.VerifyEmail(now);
        }
        // (else: already verified — idempotent success, no domain change needed)

        token.MarkUsed(now);

        // ----- 4. Audit -----
        await _audit.RecordAsync(
            action: AuditAction.UserEmailVerified,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email },
            ct: ct);

        await _db.SaveChangesAsync(ct);

        return new VerifyEmailResponse(
            UserId: user.Id,
            Email: user.Email,
            Status: user.Status.ToSnakeCase());
    }
}
