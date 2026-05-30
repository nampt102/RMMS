using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Security;
using Rmms.Application.Email;
using Rmms.Domain.Auth;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.Auth.ForgotPassword;

/// <summary>
/// Handles <see cref="ForgotPasswordCommand"/>:
///   1. Lookup user by email.
///   2. If user exists AND status=Active → issue token + send email.
///   3. If user doesn't exist OR is unverified/inactive → log audit but return success
///      (don't leak existence of accounts via timing OR error code).
///   4. Audit <see cref="AuditAction.UserPasswordResetRequested"/> in either case.
///
/// Token is single-use, 24h TTL, SHA-256 hashed. Plaintext sent in email only.
/// </summary>
internal sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ForgotPasswordCommandHandler(
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

    public async ValueTask<Result> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var now = _clock.UtcNow;

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null)
        {
            // Don't reveal — audit silent failure, return success.
            await _audit.RecordAsync(
                action: AuditAction.UserPasswordResetRequested,
                targetEntity: "auth",
                targetId: null,
                metadata: new { email = normalizedEmail, status = "user_not_found" },
                ct: ct);
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        if (user.Status != UserStatus.Active)
        {
            // Same — silent. Don't leak that account is inactive / pending.
            await _audit.RecordAsync(
                action: AuditAction.UserPasswordResetRequested,
                targetEntity: "user",
                targetId: user.Id,
                metadata: new { user.Email, status = "skipped", reason = user.Status.ToString().ToLowerInvariant() },
                ct: ct);
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        // Issue token
        var (plaintext, hash) = OpaqueToken.Generate();
        var token = PasswordResetToken.Issue(user.Id, hash, now, TokenLifetime);
        _db.PasswordResetTokens.Add(token);

        // Send email
        var message = _templates.BuildPasswordReset(
            toEmail: user.Email,
            toName: user.FullName,
            tokenPlaintext: plaintext,
            language: user.PreferredLanguage);

        await _emailSender.SendAsync(message, ct);

        // Audit
        await _audit.RecordAsync(
            action: AuditAction.UserPasswordResetRequested,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, token_id = token.Id, expires_at = token.ExpiresAt },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
