using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Enums;

namespace Rmms.UnitTests.Common;

/// <summary>Configurable face verifier for tests — defaults to a successful match.</summary>
internal sealed class FakeFaceVerificationService : IFaceVerificationService
{
    private readonly FaceVerificationResult _result;
    private readonly decimal? _confidence;

    public FakeFaceVerificationService(
        FaceVerificationResult result = FaceVerificationResult.Success, decimal? confidence = 0.99m)
    {
        _result = result;
        _confidence = confidence;
    }

    public Task<FaceVerificationOutcome> VerifyAsync(Guid userId, PhotoUpload? selfie, CancellationToken ct = default) =>
        Task.FromResult(new FaceVerificationOutcome(_result, _confidence));
}
