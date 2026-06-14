using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.FaceRecognition;

/// <summary>
/// Self-service removal of the caller's own face enrollment (M06): delete the engine
/// subject + clear the enrollment. The user must enroll again before their next
/// check-in passes face verification (BR-206). Mirrors <see cref="AdminRemoveFaceCommand"/>
/// but scoped to the caller, so no elevated authorization is required.
/// </summary>
public sealed record RemoveMyFaceCommand(Guid UserId) : IRequest<Result>;

internal sealed class RemoveMyFaceCommandHandler : IRequestHandler<RemoveMyFaceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IFaceClient _faceClient;
    private readonly IAuditLogger _audit;

    public RemoveMyFaceCommandHandler(IAppDbContext db, IFaceClient faceClient, IAuditLogger audit)
    {
        _db = db;
        _faceClient = faceClient;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(RemoveMyFaceCommand command, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy người dùng."));
        }

        if (user.FaceTemplateExternalId is { } subject)
        {
            try
            {
                await _faceClient.DeleteAsync(subject, ct);
            }
            catch (Exception)
            {
                return Result.Failure(
                    new Error(ErrorCodes.UpstreamUnavailable, "Dịch vụ nhận diện khuôn mặt không phản hồi."));
            }
            user.ClearFaceEnrollment();
        }

        await _audit.RecordAsync(AuditAction.FaceRemoved, "user", user.Id,
            new { user.Id, self = true }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
