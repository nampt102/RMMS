using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Admin.Users.AdminResetPassword;

/// <summary>
/// Admin force-issues a password reset link for a target user.
/// Behaves like forgot-password but identifies by user_id (Admin doesn't need to know email).
/// Returns success even if user is inactive — Admin sees confirmation.
/// </summary>
public sealed record AdminResetPasswordCommand(Guid UserId) : IRequest<Result>;
