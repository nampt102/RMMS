using System.Security.Cryptography;
using System.Text;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Identity;

/// <summary>
/// Opaque refresh tokens: 256-bit cryptographically random, base64url-encoded.
/// Only SHA-256 hash is stored — see <c>RefreshToken.TokenHash</c>.
/// </summary>
internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public GeneratedRefreshToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256 bits
        var plaintext = Base64Url(bytes);
        return new GeneratedRefreshToken(plaintext, Hash(plaintext));
    }

    public string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        // Hex lowercase — matches column length 64 in EF config.
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
