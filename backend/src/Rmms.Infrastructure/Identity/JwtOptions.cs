namespace Rmms.Infrastructure.Identity;

/// <summary>
/// Bound from <c>appsettings.json</c> section <c>Jwt</c>.
/// Per <c>05-api-conventions.md</c>: HS256, 15-min access, 30-day refresh.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HS256 signing key. MUST be at least 32 bytes (256 bits) — validated at startup.
    /// Single key in Sprint 01 (ADR-010 TODO). Sprint 03+ may add rotation.
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Token <c>iss</c> claim — e.g., <c>rmms.local</c>.</summary>
    public string Issuer { get; init; } = "rmms.local";

    /// <summary>Token <c>aud</c> claim — e.g., <c>rmms.clients</c>.</summary>
    public string Audience { get; init; } = "rmms.clients";

    /// <summary>Access token lifetime in minutes. Default 15 per spec.</summary>
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days. Default 30 per spec.</summary>
    public int RefreshTokenDays { get; init; } = 30;
}
