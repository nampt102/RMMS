using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Email;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;
using Rmms.Shared.Errors;

namespace Rmms.Application.Admin.Users.CreateAdminUser;

/// <summary>
/// Handles <see cref="CreateAdminUserCommand"/>:
///   1. Validate role ≠ pg.
///   2. Lookup existing email; conflict if already registered.
///   3. Generate strong random initial password (12 chars, letters+digits) — sent in email.
///   4. <c>User.CreateByAdmin</c> — status starts <c>Active</c>, no email verification needed.
///   5. Audit <see cref="AuditAction.UserCreatedByAdmin"/>.
///   6. Email user the initial password.
///   7. SaveChanges atomically.
/// </summary>
internal sealed class CreateAdminUserCommandHandler : IRequestHandler<CreateAdminUserCommand, Result<AdminUserDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IAuditLogger _audit;

    public CreateAdminUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IEmailSender emailSender,
        IEmailTemplateRenderer templates,
        IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _emailSender = emailSender;
        _templates = templates;
        _audit = audit;
    }

    public async ValueTask<Result<AdminUserDto>> Handle(CreateAdminUserCommand command, CancellationToken ct)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        // Uniqueness
        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail, ct);
        if (emailTaken)
        {
            return Result.Failure<AdminUserDto>(
                Error.Conflict(ErrorCodes.EmailAlreadyRegistered, "Email đã được đăng ký."));
        }

        // Parse role
        var role = command.Role switch
        {
            "leader" => UserRole.Leader,
            "buh" => UserRole.Buh,
            "admin" => UserRole.Admin,
            _ => throw new InvalidOperationException($"Invalid role for admin-create: {command.Role}"),
        };

        // Initial password — 12 chars, alphanumeric (no ambiguous chars like 0/O/l/1)
        var initialPassword = GenerateInitialPassword();
        var passwordHash = _hasher.Hash(initialPassword);

        var user = User.CreateByAdmin(
            email: normalizedEmail,
            passwordHash: passwordHash,
            fullName: command.FullName,
            role: role,
            phone: command.Phone,
            preferredLanguage: command.PreferredLanguage);

        _db.Users.Add(user);

        // Audit
        await _audit.RecordAsync(
            action: AuditAction.UserCreatedByAdmin,
            targetEntity: "user",
            targetId: user.Id,
            metadata: new { user.Email, role = command.Role },
            ct: ct);

        // Email user with initial credentials
        var message = _templates.BuildAdminCreatedAccount(
            toEmail: user.Email,
            toName: user.FullName,
            initialPassword: initialPassword,
            role: command.Role,
            language: user.PreferredLanguage);
        await _emailSender.SendAsync(message, ct);

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
            UpdatedAt: user.UpdatedAt);
    }

    /// <summary>Random 12-char password, alphanumeric, no ambiguous chars.</summary>
    private static string GenerateInitialPassword()
    {
        // Excludes 0/O/1/l/I to make read-aloud copy safer.
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);

        var sb = new StringBuilder(12);
        for (var i = 0; i < bytes.Length; i++)
        {
            sb.Append(alphabet[bytes[i] % alphabet.Length]);
        }
        // Guarantee at least 1 letter + 1 digit (replace last 2 chars).
        sb[10] = 'A';
        sb[11] = '7';
        return sb.ToString();
    }
}
