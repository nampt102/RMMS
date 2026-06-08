namespace Rmms.Infrastructure.Notifications;

/// <summary>
/// Bound from <c>appsettings.json</c> section <c>Push</c> (M14).
/// </summary>
public sealed class PushOptions
{
    public const string SectionName = "Push";

    /// <summary><c>console</c> (Dev/CI, logs only) or <c>fcm</c> (real Firebase HTTP v1).</summary>
    public string Provider { get; init; } = "console";

    /// <summary>
    /// Path to the Firebase service-account JSON. When empty, the Admin SDK falls back to
    /// the ambient <c>GOOGLE_APPLICATION_CREDENTIALS</c> env var. Keep the file out of git —
    /// inject via env / secret mount in real environments.
    /// </summary>
    public string CredentialsPath { get; init; } = string.Empty;

    public bool IsFcm => string.Equals(Provider, "fcm", StringComparison.OrdinalIgnoreCase);
}
