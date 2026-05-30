using Mediator;
using Rmms.Application.Admin.Users;
using Rmms.Domain.Common;

namespace Rmms.Application.Admin.Users.CreateAdminUser;

/// <summary>
/// Admin creates Leader/BUH/Admin (BR-102/103/104). PG accounts MUST self-register; this command rejects role=pg.
/// A random initial password is generated server-side and emailed to the user;
/// user is forced to change it on first login (Phase 2 enforcement).
/// </summary>
public sealed record CreateAdminUserCommand(
    string Email,
    string FullName,
    string? Phone,
    string Role,                // "leader" | "buh" | "admin"
    string PreferredLanguage)
    : IRequest<Result<AdminUserDto>>;
