namespace Rmms.Infrastructure.Email;

/// <summary>
/// Bound from <c>appsettings.json</c> section <c>App</c>.
/// Used to build URLs in email bodies (verify, reset, login).
/// </summary>
public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Base URL used in transactional email links. Different per environment:
    ///   - Dev: <c>http://localhost:8080</c> (api host) or <c>http://localhost:3000</c> (web)
    ///   - Staging: <c>https://app-staging.rmms.example.com</c>
    ///   - Prod: <c>https://app.rmms.example.com</c>
    /// </summary>
    public string AppBaseUrl { get; init; } = "http://localhost:3000";
}
