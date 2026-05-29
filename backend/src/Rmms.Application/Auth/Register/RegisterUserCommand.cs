using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Register;

/// <summary>
/// PG self-registration command per BR-101 in <c>06-business-rules.md</c>.
/// User starts in <c>PendingEmailVerify</c> status; <see cref="VerifyEmail.VerifyEmailCommand"/> activates them.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    string? Phone,
    string PreferredLanguage)
    : IRequest<Result<RegisterUserResponse>>;
