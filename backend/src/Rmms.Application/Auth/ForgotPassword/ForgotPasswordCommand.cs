using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.ForgotPassword;

/// <summary>
/// User requests a password reset link by email. Always returns success
/// (never reveals whether the email is registered) per security best practice.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
