namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Maintenance routine (M01, Sprint 01 Day 6): hard-deletes spent / expired auth tokens.
/// Scheduled hourly by the Hangfire worker (<c>Rmms.Worker</c>).
///
/// Deletes:
///   - email-verification tokens that are used or past their 24h TTL,
///   - password-reset tokens that are used or past their 24h TTL,
///   - refresh tokens past their 30-day expiry (revoked-but-unexpired rows are kept so
///     reuse detection still works inside their window — see <c>RefreshToken</c>).
/// </summary>
public interface ITokenCleanupService
{
    Task<TokenCleanupResult> RunAsync(CancellationToken ct = default);
}

/// <summary>Per-run delete counts, surfaced for logging + tests.</summary>
public sealed record TokenCleanupResult(
    int EmailVerificationTokens,
    int PasswordResetTokens,
    int RefreshTokens)
{
    public int Total => EmailVerificationTokens + PasswordResetTokens + RefreshTokens;
}
