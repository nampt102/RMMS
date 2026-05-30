namespace Rmms.Application.Auth.Login;

/// <summary>
/// Returned to client on successful login.
///
/// - <c>AccessToken</c>: JWT HS256, 15 min lifetime per spec.
/// - <c>RefreshToken</c>: opaque 256-bit, 30 day lifetime per spec.
///   Plaintext is returned ONCE here; only the SHA-256 hash is stored server-side.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    LoginUserInfo User);

public sealed record LoginUserInfo(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Status,
    string PreferredLanguage);
