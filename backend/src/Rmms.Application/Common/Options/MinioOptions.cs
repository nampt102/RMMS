namespace Rmms.Application.Common.Options;

/// <summary>
/// MinIO / S3 object storage settings bound from <c>appsettings.json</c> section <c>MinIO</c>.
/// Used for attendance selfies + store photos (M05/M13). Lives in Application so handlers can
/// reference defaults without depending on Infrastructure; Infrastructure binds + uses it.
/// </summary>
public sealed class MinioOptions
{
    public const string SectionName = "MinIO";

    /// <summary>Host:port of the S3 API used by the server to upload (e.g. <c>minio:9000</c> in Docker).</summary>
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "rmms";
    public bool UseSsl { get; init; }

    /// <summary>
    /// Browser-reachable host:port used to MINT presigned GET URLs (e.g. <c>localhost:9000</c> in dev,
    /// or <c>minio.example.com</c> in prod). The presign signature is host-bound, so when the server
    /// reaches MinIO at an internal name (<c>minio:9000</c>) the URL must be signed for the public host
    /// instead — otherwise the web admin browser can't load the image. Empty → reuse <see cref="Endpoint"/>.
    /// </summary>
    public string PublicEndpoint { get; init; } = string.Empty;

    /// <summary>Whether the public endpoint is HTTPS (null → falls back to <see cref="UseSsl"/>).</summary>
    public bool? PublicUseSsl { get; init; }

    /// <summary>Presigned GET URL lifetime (seconds) for photo previews. Default 1h.</summary>
    public int PresignedUrlTtlSeconds { get; init; } = 3600;
}
