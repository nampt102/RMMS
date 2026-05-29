using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Identity;

/// <summary>
/// Issues HS256 JWT access tokens per <c>05-api-conventions.md</c>:
/// <code>
/// {
///   "sub": "user_id",
///   "email": "...",
///   "role": "pg|leader|admin|buh",
///   "device_id": "...",
///   "iat": ..., "exp": ..., "iss": "rmms-api", "aud": "rmms-clients"
/// }
/// </code>
/// </summary>
internal sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                $"Configuration {JwtOptions.SectionName}:SigningKey must be at least 32 bytes (256 bits) for HS256.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public IssuedAccessToken IssueAccess(Guid userId, string email, UserRole role, Guid deviceId, DateTimeOffset now)
    {
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("role", role.ToString().ToLowerInvariant()),
            new("device_id", deviceId.ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.WriteToken(token);

        return new IssuedAccessToken(jwt, expires);
    }
}
