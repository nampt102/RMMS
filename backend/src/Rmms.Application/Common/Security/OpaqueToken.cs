using System.Security.Cryptography;
using System.Text;

namespace Rmms.Application.Common.Security;

/// <summary>
/// Helper for cryptographically random opaque tokens used by email verification,
/// password reset, and similar single-use links.
///
/// - 256-bit random source (<see cref="RandomNumberGenerator.GetBytes(int)"/>).
/// - Plaintext is base64url-encoded → URL-safe.
/// - Hash is SHA-256 hex lowercase, 64 chars — matches DB column length.
/// Only the hash is persisted; plaintext leaves the server exactly once (in the email URL).
///
/// Pattern matches <c>RefreshTokenGenerator</c> deliberately — same algorithm,
/// different DTOs to make intent explicit per consumer.
/// </summary>
public static class OpaqueToken
{
    /// <summary>Generate a new (plaintext, hash) pair.</summary>
    public static (string Plaintext, string Hash) Generate()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);

        var plaintext = Base64Url(bytes);
        return (plaintext, Hash(plaintext));
    }

    /// <summary>Hash a known plaintext token for DB lookup on /verify-email or /reset-password.</summary>
    public static string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        Span<byte> hashBytes = stackalloc byte[32];
        var success = SHA256.TryHashData(Encoding.UTF8.GetBytes(plaintext), hashBytes, out _);
        if (!success)
        {
            throw new InvalidOperationException("SHA-256 hashing failed unexpectedly.");
        }
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
