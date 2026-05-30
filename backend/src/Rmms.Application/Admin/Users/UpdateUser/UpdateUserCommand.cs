using Mediator;
using Rmms.Application.Admin.Users;
using Rmms.Domain.Common;

namespace Rmms.Application.Admin.Users.UpdateUser;

/// <summary>
/// Admin updates a user — status toggle + profile (full name + phone + language).
/// Email + role are immutable post-creation (re-create the user if either needs to change).
///
/// When status transitions <c>active → inactive</c>, ALL refresh tokens for the user are revoked.
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string? FullName,
    string? Phone,
    string? Status,
    string? PreferredLanguage)
    : IRequest<Result<AdminUserDto>>;
