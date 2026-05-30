using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.ResetPassword;

/// <summary>
/// Apply new password using a single-use reset token (from email).
/// After success: token marked used, ALL user's active refresh tokens revoked
/// (force re-login on every device for safety).
/// </summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
