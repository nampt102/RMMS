using Rmms.Domain.Enums;

namespace Rmms.Application.Common.Abstractions;

/// <summary>Result of a face match: outcome + confidence (0..1 from the provider).</summary>
public sealed record FaceVerificationOutcome(FaceVerificationResult Result, decimal? Confidence);

/// <summary>
/// Biometric Face Verification at check-in/check-out (BR-206, M05). Phase 1A ships a stub
/// (always <see cref="FaceVerificationResult.Success"/>) — M06 swaps in the FPT.AI Face client
/// (Refit + Polly). The attendance state machine already handles a <c>Fail</c> outcome, so no
/// handler changes are needed when the real provider lands.
/// </summary>
public interface IFaceVerificationService
{
    Task<FaceVerificationOutcome> VerifyAsync(Guid userId, PhotoUpload? selfie, CancellationToken ct = default);
}
