using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Security;
using Rmms.Application.Email;
using Rmms.Domain.Auth;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;
using Rmms.Shared.Errors;

namespace Rmms.Application.Auth.Register;

/// <summary>
/// Handles <see cref="RegisterUserCommand"/>:
///   1. Normalize email (lowercase, trim).
///   2. Check uniqueness — returns <see cref="ErrorCodes.EmailAlreadyRegistered"/> if taken.
///   3. Hash password (BCrypt cost 12 via <see cref="IPasswordHasher"/>).
///   4. Create <see cref="User"/> via <see cref="User.Register"/> (status = PendingEmailVerify).
///   5. Issue <see cref="EmailVerificationToken"/> (24h TTL, SHA-256 hashed).
///   6. Send verification email (Console in Dev, SendGrid in Staging+).
///   7. Emit <see cref="AuditAction.UserRegistered"/> audit log.
///   8. SaveChanges atomically.
///
/// Errors are returned via <see cref="Result"/>; only truly unexpected conditions throw.
/// </summary>
internal sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private static readonly TimeSpan EmailTokenLifetime = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public RegisterUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IEmailSender emailSender,
        IEmailTemplateRenderer templates,
        IAuditLogger audit,
        IDateTimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _emailSender = emailSender;
        _templates = templates;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<RegisterUserResponse>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        // ----- 1. Uniqueness check (includes soft-deleted via IgnoreQueryFilters
        //          so a deactivated user can't have their email re-registered) -----
        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail, ct);

        if (emailTaken)
        {
            return Result.Failure<RegisterUserResponse>(
                Error.Conflict(ErrorCodes.EmailAlreadyRegistered, "Email đã được đăng ký."));
        }

        // ----- 2. Create user -----
        var passwordHash = _hasher.Hash(command.Password);
        var user = User.Register(
            email: normalizedEmail,
            passwordHash: passwordHash,
            fullName: command.FullName,
            phone: command.Phone,
            preferredLanguage: command.PreferredLanguage);

        _db.Users.Add(user);

        // ----- 3. Issue email verification token -----
        var (plaintext, hash) = OpaqueToken.Generate();
        var token = EmailVerificationToken.Issue(
            userId: user.Id,
            tokenHash: hash,
            now: _clock.UtcNow,
            lifetime: EmailTokenLifetime);

        _db.EmailVerificationTokens.Add(token);

        // ----- 4. Send verification email -----
        var email = _templates.BuildVerifyEmail(
            toEmail: user.Email,
            toName: user.FullName,
            tokenPlaintext: plaintext,
            language: user.PreferredLanguage);

        await _emailSender.SendAsync(email, ct);

        // ----- 5. Audit -----
        await _audit.RecordAsync(
            action: AuditAction.UserRegistered,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, user.Role, source = "self_registration" },
            ct: ct);

        // ----- 6. Persist atomically -----
        await _db.SaveChangesAsync(ct);

        return new RegisterUserResponse(
            UserId: user.Id,
            Email: user.Email,
            Status: user.Status.ToSnakeCase());
    }
}
