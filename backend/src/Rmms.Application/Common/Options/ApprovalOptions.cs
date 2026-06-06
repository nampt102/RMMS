namespace Rmms.Application.Common.Options;

/// <summary>
/// Approval workflow config (M09). <see cref="SigningKey"/> is the server-side HMAC
/// secret for BUH email-link tokens — set it via env / user-secrets, never commit.
/// </summary>
public sealed class ApprovalOptions
{
    public const string SectionName = "Approval";

    /// <summary>HMAC-SHA256 secret for signing email-link tokens (BR-407).</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Email-link token lifetime (AC-18: 24h).</summary>
    public int TokenTtlHours { get; init; } = 24;

    /// <summary>Public web path that renders the email-link landing page (token appended as ?token=).</summary>
    public string WebApprovalPath { get; init; } = "/approve";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SigningKey);
}
