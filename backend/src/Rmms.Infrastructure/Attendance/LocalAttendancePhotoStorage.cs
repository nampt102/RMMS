using System.Globalization;
using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Common;

namespace Rmms.Infrastructure.Attendance;

/// <summary>
/// Phase 1A stub photo storage (M13/MinIO deferred). Does not persist bytes — returns a
/// deterministic placeholder URL so attendance records carry a stable, inspectable reference.
/// TODO(M13): replace with the MinIO client (PutObject + presigned GET URL).
/// </summary>
internal sealed class LocalAttendancePhotoStorage : IAttendancePhotoStorage
{
    public Task<string> SaveAsync(Guid userId, string kind, PhotoUpload photo, CancellationToken ct = default)
    {
        // Stable, opaque reference: stub URL keyed by user + slot + a fresh id.
        var objectId = UuidV7.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var url = $"local://attendance/{userId:N}/{kind}/{objectId}";
        return Task.FromResult(url);
    }
}
