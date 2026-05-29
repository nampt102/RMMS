namespace Rmms.Infrastructure.Email;

/// <summary>
/// Bound from <c>appsettings.json</c> section <c>Email</c>.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><c>Console</c> (Dev/CI) or <c>SendGrid</c> (Staging/Prod, wired Day 8).</summary>
    public string Provider { get; init; } = "Console";

    /// <summary>Display name used in email From + body branding. Default: <c>RMMS</c>.</summary>
    public string BrandName { get; init; } = "RMMS";

    /// <summary>SendGrid-only: sender email address.</summary>
    public string FromEmail { get; init; } = "noreply@rmms.local";

    /// <summary>SendGrid-only: sender display name.</summary>
    public string FromName { get; init; } = "RMMS";
}
