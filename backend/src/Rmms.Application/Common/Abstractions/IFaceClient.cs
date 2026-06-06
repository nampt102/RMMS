namespace Rmms.Application.Common.Abstractions;

/// <summary>Face verification outcome: whether the candidate matches the enrolled subject + confidence (0..1).</summary>
public sealed record FaceMatchResult(bool IsMatch, decimal Confidence);

/// <summary>
/// Port over the face-recognition engine (M06, ADR-011 — self-hosted CompreFace). Embeddings are
/// owned by the engine under a <c>subject</c> id (the RMMS user id); our DB keeps only that id.
///
/// A deterministic dev client is the default until <c>CompreFace:ApiKey</c> is configured —
/// mirroring the SendGrid / FCM gating pattern — so the whole enroll → verify flow is exercisable
/// without the service (local / CI / unit tests). Confidence threshold lives in <c>CompreFaceOptions</c>.
/// </summary>
public interface IFaceClient
{
    /// <summary>True when backed by the real CompreFace service (vs the deterministic dev client).</summary>
    bool IsLive { get; }

    /// <summary>Enroll one or more face images under <paramref name="subjectId"/> (CompreFace recognition collection).</summary>
    Task EnrollAsync(string subjectId, IReadOnlyList<PhotoUpload> photos, CancellationToken ct = default);

    /// <summary>Verify a live selfie against the enrolled <paramref name="subjectId"/> (recognize + threshold match).</summary>
    Task<FaceMatchResult> VerifyAsync(string subjectId, PhotoUpload selfie, CancellationToken ct = default);

    /// <summary>Remove a subject and its enrolled faces (admin delete / re-enroll trigger).</summary>
    Task DeleteAsync(string subjectId, CancellationToken ct = default);
}
