using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.FaceRecognition;

/// <summary>
/// Enroll the caller's face (M06, ADR-011): push the angle photos to the face engine under
/// <c>subject = userId</c> and record the enrollment on the user. Re-enrolling first clears the
/// previous subject so the collection is replaced, not appended. Identity comes from the JWT.
/// </summary>
public sealed record EnrollFaceCommand(Guid UserId, IReadOnlyList<PhotoUpload> Photos)
    : IRequest<Result<FaceStatusDto>>;

public sealed class EnrollFaceCommandValidator : AbstractValidator<EnrollFaceCommand>
{
    public EnrollFaceCommandValidator()
    {
        RuleFor(x => x.Photos).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.Photos.Count).LessThanOrEqualTo(5).WithErrorCode("INVALID_VALUE");
    }
}

internal sealed class EnrollFaceCommandHandler : IRequestHandler<EnrollFaceCommand, Result<FaceStatusDto>>
{
    private readonly IAppDbContext _db;
    private readonly IFaceClient _faceClient;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public EnrollFaceCommandHandler(IAppDbContext db, IFaceClient faceClient, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _faceClient = faceClient;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<FaceStatusDto>> Handle(EnrollFaceCommand command, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            return Result.Failure<FaceStatusDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy người dùng."));
        }

        var subject = user.Id.ToString();
        try
        {
            // Replace any prior enrollment so the engine collection is clean.
            await _faceClient.DeleteAsync(subject, ct);
            await _faceClient.EnrollAsync(subject, command.Photos, ct);
        }
        catch (Exception)
        {
            return Result.Failure<FaceStatusDto>(
                new Error(ErrorCodes.UpstreamUnavailable, "Dịch vụ nhận diện khuôn mặt không phản hồi. Vui lòng thử lại."));
        }

        user.RecordFaceEnrollment(subject, _clock.UtcNow);
        await _audit.RecordAsync(AuditAction.FaceEnrolled, "user", user.Id,
            new { user.Id, photos = command.Photos.Count }, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new FaceStatusDto(true, user.FaceEnrolledAt));
    }
}
