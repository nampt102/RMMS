using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>Photo storage stub for tests — returns a deterministic fake URL per slot.</summary>
internal sealed class FakePhotoStorage : IAttendancePhotoStorage
{
    public Task<string> SaveAsync(Guid userId, string kind, PhotoUpload photo, CancellationToken ct = default) =>
        Task.FromResult($"test://{kind}/{userId:N}");
}
