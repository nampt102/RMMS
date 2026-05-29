namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// One-way password hashing.
/// Sprint 01 implementation: BCrypt cost 12 (see <c>Rmms.Infrastructure.Identity.BCryptPasswordHasher</c>).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hash a plaintext password using a salted, slow KDF.</summary>
    string Hash(string plaintext);

    /// <summary>Constant-time compare. Returns <c>true</c> if <paramref name="plaintext"/> matches <paramref name="hash"/>.</summary>
    bool Verify(string plaintext, string hash);

    /// <summary>
    /// True if the hash uses an older work factor than current — caller should re-hash on next login.
    /// Lets us upgrade BCrypt cost without forcing password resets.
    /// </summary>
    bool NeedsRehash(string hash);
}
