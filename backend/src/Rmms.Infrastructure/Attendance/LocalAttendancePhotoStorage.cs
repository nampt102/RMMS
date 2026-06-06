using System.Globalization;
using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Common;

namespace Rmms.Infrastructure.Attendance;

/// <summary>
/// No-op photo storage fallback (used when MinIO is not configured, e.g. some test/CI envs).
/// Does not persist bytes — returns a deterministic <c>local://</c> placeholder key that read
/// paths pass through unchanged. Prefer <see cref="MinioAttendancePhotoStorage"/> in real envs.
/// </summary>
internal sealed class LocalAttendancePhotoStorage : IAttendancePhotoStorage
{
    public Task<string> SaveAsync(Guid userId, string kind, PhotoUpload photo, CancellationToken ct = default)
    {
        var objectId = UuidV7.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        return Task.FromResult($"local://attendance/{userId:N}/{kind}/{objectId}");
    }

    public Task<string?> GetUrlAsync(string? storedKey, CancellationToken ct = default) =>
        Task.FromResult(storedKey);

    public Task DeleteAsync(string? storedKey, CancellationToken ct = default) => Task.CompletedTask;
}
