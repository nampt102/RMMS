namespace Rmms.Application.Common.Options;

/// <summary>
/// JWT + refresh-token settings bound from <c>appsettings.json</c> section <c>Jwt</c>.
/// Per <c>knowledge-base/05-api-conventions.md</c> — HS256 access (15 min) + refresh (30 day).
///
/// Lives in Application layer so handlers can read lifetime config without
/// depending on Infrastructure. Infrastructure binds + validates this class.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 signing key — MUST be at least 32 bytes. Validated at startup.</summary>
    public string SigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = "rmms.local";
    public string Audience { get; init; } = "rmms.clients";

    /// <summary>Access token lifetime in minutes. Default 15.</summary>
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days. Default 30.</summary>
    public int RefreshTokenDays { get; init; } = 30;
}
