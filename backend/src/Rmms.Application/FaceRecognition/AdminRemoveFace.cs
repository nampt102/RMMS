using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.FaceRecognition;

/// <summary>
/// Admin removes a user's face enrollment (M06): delete the engine subject + clear the user's
/// enrollment. With <paramref name="ReEnroll"/> this is the "force re-enroll" trigger — the user
/// must enroll again before their next check-in passes face (BR-206).
/// </summary>
public sealed record AdminRemoveFaceCommand(Guid TargetUserId, Guid AdminUserId, bool ReEnroll) : IRequest<Result>;

internal sealed class AdminRemoveFaceCommandHandler : IRequestHandler<AdminRemoveFaceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IFaceClient _faceClient;
    private readonly IAuditLogger _audit;

    public AdminRemoveFaceCommandHandler(IAppDbContext db, IFaceClient faceClient, IAuditLogger audit)
    {
        _db = db;
        _faceClient = faceClient;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(AdminRemoveFaceCommand command, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.TargetUserId, ct);
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
            new { user.Id, admin = command.AdminUserId, reEnroll = command.ReEnroll }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
