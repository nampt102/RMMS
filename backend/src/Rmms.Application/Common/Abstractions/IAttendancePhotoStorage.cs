namespace Rmms.Application.Common.Abstractions;

/// <summary>An in-memory photo payload (selfie / store photo) handed to storage.</summary>
public sealed record PhotoUpload(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Stores attendance selfies / store photos and returns a retrievable URL (M05). Phase 1A ships
/// a local stub that returns a deterministic placeholder URL — M13 swaps in the MinIO client.
/// <paramref name="kind"/> labels the slot (e.g. <c>check_in_selfie</c>) for path layout.
/// </summary>
public interface IAttendancePhotoStorage
{
    Task<string> SaveAsync(Guid userId, string kind, PhotoUpload photo, CancellationToken ct = default);
}
