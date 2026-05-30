using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;

namespace Rmms.Application.Maintenance;

/// <inheritdoc cref="ITokenCleanupService" />
internal sealed class TokenCleanupService : ITokenCleanupService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(IAppDbContext db, IDateTimeProvider clock, ILogger<TokenCleanupService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TokenCleanupResult> RunAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        // Tokens are plain entities (not soft-deleted) → RemoveRange hard-deletes.
        // Cleanup volume is small (24h / 30d TTLs, hourly cadence), so loading the
        // matching rows is fine; switch to ExecuteDelete only if volumes ever grow.
        var emailTokens = await _db.EmailVerificationTokens
            .Where(t => t.UsedAt != null || t.ExpiresAt < now)
            .ToListAsync(ct);
        _db.EmailVerificationTokens.RemoveRange(emailTokens);

        var resetTokens = await _db.PasswordResetTokens
            .Where(t => t.UsedAt != null || t.ExpiresAt < now)
            .ToListAsync(ct);
        _db.PasswordResetTokens.RemoveRange(resetTokens);

        // Expired refresh tokens only. Revoked-but-unexpired rows stay so reuse detection
        // keeps working within their 30-day window (see RefreshToken docs).
        var refreshTokens = await _db.RefreshTokens
            .Where(t => t.ExpiresAt < now)
            .ToListAsync(ct);
        _db.RefreshTokens.RemoveRange(refreshTokens);

        await _db.SaveChangesAsync(ct);

        var result = new TokenCleanupResult(emailTokens.Count, resetTokens.Count, refreshTokens.Count);
        if (result.Total > 0)
        {
            _logger.LogInformation(
                "Auth token cleanup removed {Email} email-verification + {Reset} password-reset + {Refresh} refresh tokens.",
                result.EmailVerificationTokens, result.PasswordResetTokens, result.RefreshTokens);
        }

        return result;
    }
}
