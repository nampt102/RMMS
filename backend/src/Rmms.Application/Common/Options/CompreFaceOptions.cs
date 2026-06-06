namespace Rmms.Application.Common.Options;

/// <summary>
/// Self-hosted CompreFace settings bound from <c>appsettings.json</c> section <c>CompreFace</c>
/// (M06, ADR-011). When <see cref="ApiKey"/> is blank the app falls back to the deterministic dev
/// face client (no service calls). Lives in Application so handlers can read the threshold.
/// </summary>
public sealed class CompreFaceOptions
{
    public const string SectionName = "CompreFace";

    /// <summary>Base URL of the CompreFace gateway, e.g. <c>http://compreface-fe:8080</c>.</summary>
    public string BaseUrl { get; init; } = "http://compreface-fe:8080";

    /// <summary>API key of a CompreFace <em>Recognition</em> service (created in its UI).</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Minimum similarity (0..1) to treat as a match. Default 0.85 (tuned during S4).</summary>
    public decimal ConfidenceThreshold { get; init; } = 0.85m;

    /// <summary>True when a real key is configured (else use the dev face client).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
