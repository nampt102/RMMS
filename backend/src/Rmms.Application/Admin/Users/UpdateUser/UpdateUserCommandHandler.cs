using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Admin.Users.UpdateUser;

internal sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<AdminUserDto>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public UpdateUserCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<AdminUserDto>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            return Result.Failure<AdminUserDto>(
                Error.NotFound(ErrorCodes.NotFound, "Người dùng không tồn tại."));
        }

        var oldStatus = user.Status;

        // Profile updates
        if (command.FullName is not null || command.Phone is not null || command.PreferredLanguage is not null)
        {
            user.UpdateProfile(command.FullName, command.Phone, command.PreferredLanguage);
        }

        // Status transition
        if (command.Status is not null)
        {
            switch (command.Status)
            {
                case "active":
                    if (user.Status == UserStatus.PendingEmailVerify)
                    {
                        return Result.Failure<AdminUserDto>(
                            Error.Validation(ErrorCodes.ValidationFailed,
                                "Người dùng chưa xác minh email — không thể kích hoạt trực tiếp."));
                    }
                    user.Activate();
                    break;
                case "inactive":
                    user.Deactivate();
                    break;
            }
        }

        // If user just became Inactive → revoke all refresh tokens
        if (oldStatus != UserStatus.Inactive && user.Status == UserStatus.Inactive)
        {
            var now = _clock.UtcNow;
            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in activeTokens)
            {
                t.Revoke(now);
            }
        }

        // Audit
        if (oldStatus != user.Status)
        {
            await _audit.RecordAsync(
                action: AuditAction.UserStatusChanged,
                targetEntity: "user",
                targetId: user.Id,
                metadata: new
                {
                    user.Email,
                    from = oldStatus.ToSnakeCase(),
                    to = user.Status.ToSnakeCase(),
                },
                ct: ct);
        }

        await _audit.RecordAsync(
            action: AuditAction.UserUpdatedByAdmin,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, fields = WhichFields(command) },
            ct: ct);

        await _db.SaveChangesAsync(ct);

        return new AdminUserDto(
            Id: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Phone: user.Phone,
            Role: user.Role.ToSnakeCase(),
            Status: user.Status.ToSnakeCase(),
            PreferredLanguage: user.PreferredLanguage,
            EmailVerifiedAt: user.EmailVerifiedAt,
            LastLoginAt: user.LastLoginAt,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            FaceEnrolled: user.FaceTemplateExternalId != null,
            FaceEnrolledAt: user.FaceEnrolledAt);
    }

    private static string[] WhichFields(UpdateUserCommand c)
    {
        var fields = new List<string>(4);
        if (c.FullName is not null) fields.Add("full_name");
        if (c.Phone is not null) fields.Add("phone");
        if (c.Status is not null) fields.Add("status");
        if (c.PreferredLanguage is not null) fields.Add("preferred_language");
        return fields.ToArray();
    }
}
