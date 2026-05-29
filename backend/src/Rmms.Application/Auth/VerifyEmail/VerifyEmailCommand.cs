using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.VerifyEmail;

/// <summary>
/// Confirms email ownership by exchanging the token from the verification URL
/// for a user activation. After success, user status transitions to <c>Active</c>.
///
/// Token plaintext is what's in the URL — never the hash.
/// </summary>
public sealed record VerifyEmailCommand(string Token) : IRequest<Result<VerifyEmailResponse>>;
