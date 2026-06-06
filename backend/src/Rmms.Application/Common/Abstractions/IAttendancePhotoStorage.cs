namespace Rmms.Application.Common.Abstractions;

/// <summary>An in-memory photo payload (selfie / store photo) handed to storage.</summary>
public sealed record PhotoUpload(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Stores attendance selfies / store photos in object storage (M05/M13). <see cref="SaveAsync"/>
/// returns a STABLE object key persisted on the attendance record; read paths call
/// <see cref="GetUrlAsync"/> to mint a short-lived presigned URL for preview, and the retention
/// job calls <see cref="DeleteAsync"/> (CR-4, 90-day selfie retention).
///
/// Phase 1A ships a MinIO implementation; a local stub remains for tests / no-storage envs.
/// <paramref name="kind"/> labels the slot (e.g. <c>check_in_selfie</c>) for path layout.
/// </summary>
public interface IAttendancePhotoStorage
{
    /// <summary>Upload a photo; returns the stored object key (NOT a URL).</summary>
    Task<string> SaveAsync(Guid userId, string kind, PhotoUpload photo, CancellationToken ct = default);

    /// <summary>Mint a short-lived presigned GET URL for a stored key (or null if the key is null/blank).</summary>
    Task<string?> GetUrlAsync(string? storedKey, CancellationToken ct = default);

    /// <summary>Delete a stored object (retention). No-op if the key is null/blank or already gone.</summary>
    Task DeleteAsync(string? storedKey, CancellationToken ct = default);
}
