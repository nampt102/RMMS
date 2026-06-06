using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Face;

/// <summary>
/// Deterministic dev face client (M06, ADR-011) — used when no CompreFace API key is configured
/// (local / CI / unit tests). Enrollment is a no-op success; verification always matches at 0.99
/// so the full enroll → check-in flow is exercisable without running the CompreFace service.
/// </summary>
internal sealed class DevFaceClient : IFaceClient
{
    public bool IsLive => false;

    public Task EnrollAsync(string subjectId, IReadOnlyList<PhotoUpload> photos, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<FaceMatchResult> VerifyAsync(string subjectId, PhotoUpload selfie, CancellationToken ct = default) =>
        Task.FromResult(new FaceMatchResult(IsMatch: true, Confidence: 0.99m));

    public Task DeleteAsync(string subjectId, CancellationToken ct = default) => Task.CompletedTask;
}
