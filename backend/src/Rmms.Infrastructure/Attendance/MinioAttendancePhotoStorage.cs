using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Options;
using Rmms.Domain.Common;

namespace Rmms.Infrastructure.Attendance;

/// <summary>
/// MinIO-backed attendance photo storage (M05/M13). Uploads to the configured bucket under a
/// stable key and mints short-lived presigned GET URLs for preview. The bucket is created lazily
/// on first use. Object keys (not URLs) are persisted on the attendance record.
/// </summary>
internal sealed class MinioAttendancePhotoStorage : IAttendancePhotoStorage, IDisposable
{
    private readonly IMinioClient _client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioAttendancePhotoStorage> _logger;
    private readonly SemaphoreSlim _bucketGate = new(1, 1);
    private bool _bucketReady;

    /// <summary>
    /// Dedicated client bound to <see cref="MinioOptions.PublicEndpoint"/> used ONLY to mint
    /// presigned GET URLs the browser can reach. Null when no public endpoint is configured
    /// (then the upload client's endpoint is used, which is correct for same-host dev).
    /// </summary>
    private readonly IMinioClient? _presignClient;

    public MinioAttendancePhotoStorage(
        IMinioClient client, IOptions<MinioOptions> options, ILogger<MinioAttendancePhotoStorage> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.PublicEndpoint))
        {
            _presignClient = new MinioClient()
                .WithEndpoint(_options.PublicEndpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithSSL(_options.PublicUseSsl ?? _options.UseSsl)
                .Build();
        }
    }

    public async Task<string> SaveAsync(Guid userId, string kind, PhotoUpload photo, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        var ext = ExtensionFor(photo.ContentType, photo.FileName);
        var objectId = UuidV7.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var key = $"attendance/{userId:N}/{kind}/{objectId}{ext}";

        using var stream = new MemoryStream(photo.Content, writable: false);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(key)
            .WithStreamData(stream)
            .WithObjectSize(photo.Content.LongLength)
            .WithContentType(string.IsNullOrWhiteSpace(photo.ContentType) ? "image/jpeg" : photo.ContentType), ct);

        return key;
    }

    public async Task<string?> GetUrlAsync(string? storedKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storedKey)) return null;
        // Back-compat: stub / already-absolute values are returned as-is.
        if (storedKey.StartsWith("local://", StringComparison.Ordinal) ||
            storedKey.StartsWith("http://", StringComparison.Ordinal) ||
            storedKey.StartsWith("https://", StringComparison.Ordinal))
        {
            return storedKey;
        }

        // Sign with the public-facing client when configured so the URL host is browser-reachable.
        return await (_presignClient ?? _client).PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storedKey)
            .WithExpiry(_options.PresignedUrlTtlSeconds));
    }

    public async Task DeleteAsync(string? storedKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storedKey) ||
            storedKey.StartsWith("local://", StringComparison.Ordinal) ||
            storedKey.StartsWith("http", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storedKey), ct);
        }
        catch (Exception ex)
        {
            // Retention is best-effort — log and continue (object may already be gone).
            _logger.LogWarning(ex, "Failed to delete attendance photo object {Key}", storedKey);
        }
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady) return;
        await _bucketGate.WaitAsync(ct);
        try
        {
            if (_bucketReady) return;
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.BucketName), ct);
            if (!exists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.BucketName), ct);
                _logger.LogInformation("Created MinIO bucket {Bucket}", _options.BucketName);
            }
            _bucketReady = true;
        }
        finally
        {
            _bucketGate.Release();
        }
    }

    public void Dispose()
    {
        _bucketGate.Dispose();
        _presignClient?.Dispose();
    }

    private static string ExtensionFor(string? contentType, string? fileName)
    {
        var ct = contentType?.ToLowerInvariant();
        if (ct == "image/png") return ".png";
        if (ct == "image/webp") return ".webp";
        if (ct == "image/jpeg" || ct == "image/jpg") return ".jpg";
        var dot = fileName?.LastIndexOf('.') ?? -1;
        return dot >= 0 ? fileName![dot..] : ".jpg";
    }
}
