namespace Rmms.Application.Common.Options;

/// <summary>
/// MinIO / S3 object storage settings bound from <c>appsettings.json</c> section <c>MinIO</c>.
/// Used for attendance selfies + store photos (M05/M13). Lives in Application so handlers can
/// reference defaults without depending on Infrastructure; Infrastructure binds + uses it.
/// </summary>
public sealed class MinioOptions
{
    public const string SectionName = "MinIO";

    /// <summary>Host:port of the S3 API (e.g. <c>localhost:9000</c>).</summary>
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "rmms";
    public bool UseSsl { get; init; }

    /// <summary>Presigned GET URL lifetime (seconds) for photo previews. Default 1h.</summary>
    public int PresignedUrlTtlSeconds { get; init; } = 3600;
}
