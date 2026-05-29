namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Generates opaque refresh tokens (256-bit random) + their SHA-256 hash for DB storage.
/// The plaintext is returned to the client ONCE — only the hash is persisted.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Generates a fresh token. <see cref="GeneratedRefreshToken.Plaintext"/> goes to client.</summary>
    GeneratedRefreshToken Generate();

    /// <summary>SHA-256 hash a known plaintext token — used to look up rows on <c>/auth/refresh</c>.</summary>
    string Hash(string plaintext);
}

public readonly record struct GeneratedRefreshToken(string Plaintext, string Hash);
