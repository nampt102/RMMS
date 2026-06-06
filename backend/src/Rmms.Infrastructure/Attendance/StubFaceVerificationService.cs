using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Attendance;

/// <summary>
/// Phase 1A stub Face Verification (M06 deferred). Always returns
/// <see cref="FaceVerificationResult.Success"/> at confidence 0.99 so the attendance flow is
/// fully exercisable end-to-end. TODO(M06): replace with the FPT.AI Face client.
/// </summary>
internal sealed class StubFaceVerificationService : IFaceVerificationService
{
    public Task<FaceVerificationOutcome> VerifyAsync(Guid userId, PhotoUpload? selfie, CancellationToken ct = default) =>
        Task.FromResult(new FaceVerificationOutcome(FaceVerificationResult.Success, 0.99m));
}
