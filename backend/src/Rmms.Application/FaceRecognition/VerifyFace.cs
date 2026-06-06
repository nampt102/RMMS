using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Application.FaceRecognition;

/// <summary>Standalone face verify for the caller (M06 test/aux endpoint) — uses the M05 face port.</summary>
public sealed record VerifyFaceCommand(Guid UserId, PhotoUpload Selfie) : IRequest<Result<FaceVerifyResponse>>;

public sealed class VerifyFaceCommandValidator : AbstractValidator<VerifyFaceCommand>
{
    public VerifyFaceCommandValidator() => RuleFor(x => x.Selfie).NotNull().WithErrorCode("REQUIRED");
}

internal sealed class VerifyFaceCommandHandler : IRequestHandler<VerifyFaceCommand, Result<FaceVerifyResponse>>
{
    private readonly IFaceVerificationService _face;

    public VerifyFaceCommandHandler(IFaceVerificationService face) => _face = face;

    public async ValueTask<Result<FaceVerifyResponse>> Handle(VerifyFaceCommand command, CancellationToken ct)
    {
        var outcome = await _face.VerifyAsync(command.UserId, command.Selfie, ct);
        return Result.Success(new FaceVerifyResponse(outcome.Result.ToSnakeCase(), outcome.Confidence));
    }
}

// ===== Caller's enrollment status =====

public sealed record GetFaceStatusQuery(Guid UserId) : IRequest<Result<FaceStatusDto>>;

internal sealed class GetFaceStatusQueryHandler : IRequestHandler<GetFaceStatusQuery, Result<FaceStatusDto>>
{
    private readonly IAppDbContext _db;

    public GetFaceStatusQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<FaceStatusDto>> Handle(GetFaceStatusQuery query, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new { u.FaceTemplateExternalId, u.FaceEnrolledAt })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return Result.Failure<FaceStatusDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy người dùng."));
        }

        return Result.Success(new FaceStatusDto(user.FaceTemplateExternalId is not null, user.FaceEnrolledAt));
    }
}
