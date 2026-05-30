using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.Auth.Logout;

/// <summary>
/// Idempotent — logging out an already-revoked / unknown token returns success without leaking info.
/// </summary>
internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IRefreshTokenGenerator _refreshTokenGen;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public LogoutCommandHandler(
        IAppDbContext db,
        IRefreshTokenGenerator refreshTokenGen,
        IAuditLogger audit,
        IDateTimeProvider clock)
    {
        _db = db;
        _refreshTokenGen = refreshTokenGen;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var tokenHash = _refreshTokenGen.Hash(command.RefreshToken);

        var token = await _db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (token is not null && token.RevokedAt is null)
        {
            token.Revoke(now);

            await _audit.RecordAsync(
                action: AuditAction.AuthLogout,
                targetEntity: "user",
                targetId: token.UserId,
                metadata: new { token_id = token.Id, device_id = token.DeviceId },
                ct: ct);

            await _db.SaveChangesAsync(ct);
        }
        // else: unknown or already-revoked → idempotent no-op, return success.

        return Result.Success();
    }
}
