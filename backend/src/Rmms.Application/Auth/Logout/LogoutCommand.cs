using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Logout;

/// <summary>
/// Revoke a single refresh token (logout from THIS device).
/// To revoke all sessions, call this for each refresh token OR use Admin's revoke-all endpoint (Phase 2).
/// </summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
