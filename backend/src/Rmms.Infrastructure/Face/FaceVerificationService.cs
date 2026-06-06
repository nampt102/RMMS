using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Face;

/// <summary>
/// M05 check-in/out face step (BR-206), backed by the M06 face engine via <see cref="IFaceClient"/>.
/// Resolves the user's enrolled subject and verifies the live selfie against it:
///   - not enrolled / no selfie → <see cref="FaceVerificationResult.PendingReview"/> (Admin review),
///   - engine unreachable → <see cref="FaceVerificationResult.PendingReview"/> (BR-207 / M06 edge case),
///   - otherwise success/fail by the engine match + threshold.
/// </summary>
internal sealed class FaceVerificationService : IFaceVerificationService
{
    private readonly IAppDbContext _db;
    private readonly IFaceClient _faceClient;
    private readonly ILogger<FaceVerificationService> _logger;

    public FaceVerificationService(IAppDbContext db, IFaceClient faceClient, ILogger<FaceVerificationService> logger)
    {
        _db = db;
        _faceClient = faceClient;
        _logger = logger;
    }

    public async Task<FaceVerificationOutcome> VerifyAsync(Guid userId, PhotoUpload? selfie, CancellationToken ct = default)
    {
        var subject = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FaceTemplateExternalId)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(subject) || selfie is null)
        {
            // Not enrolled (BR-206) or no selfie captured → route to Admin review, don't hard-fail.
            return new FaceVerificationOutcome(FaceVerificationResult.PendingReview, null);
        }

        try
        {
            var match = await _faceClient.VerifyAsync(subject, selfie, ct);
            return new FaceVerificationOutcome(
                match.IsMatch ? FaceVerificationResult.Success : FaceVerificationResult.Fail,
                match.Confidence);
        }
        catch (Exception ex)
        {
            // Face engine unreachable → pending review (BR-207 / M06: "Face API down").
            _logger.LogWarning(ex, "Face verification failed for user {UserId}; routing to review.", userId);
            return new FaceVerificationOutcome(FaceVerificationResult.PendingReview, null);
        }
    }
}
