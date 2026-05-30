using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Security;
using Rmms.Application.Email;
using Rmms.Domain.Auth;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Admin.Users.AdminResetPassword;

internal sealed class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, Result>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public AdminResetPasswordCommandHandler(
        IAppDbContext db,
        IEmailSender emailSender,
        IEmailTemplateRenderer templates,
        IAuditLogger audit,
        IDateTimeProvider clock)
    {
        _db = db;
        _emailSender = emailSender;
        _templates = templates;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(AdminResetPasswordCommand command, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Người dùng không tồn tại."));
        }

        var now = _clock.UtcNow;
        var (plaintext, hash) = OpaqueToken.Generate();
        var token = PasswordResetToken.Issue(user.Id, hash, now, TokenLifetime);
        _db.PasswordResetTokens.Add(token);

        var message = _templates.BuildPasswordReset(
            toEmail: user.Email,
            toName: user.FullName,
            tokenPlaintext: plaintext,
            language: user.PreferredLanguage);

        await _emailSender.SendAsync(message, ct);

        await _audit.RecordAsync(
            action: AuditAction.UserPasswordResetRequested,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, triggered_by = "admin", token_id = token.Id },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
