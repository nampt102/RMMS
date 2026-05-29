using BCrypt.Net;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Identity;

/// <summary>
/// BCrypt password hasher — cost 12 per M01 spec.
/// Spec ref: <c>knowledge-base/modules/M01-identity-access.md</c> "Password: bcrypt cost 12".
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
    }

    public bool Verify(string plaintext, string hash)
    {
        if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch (SaltParseException)
        {
            // Malformed hash in DB — treat as failure (don't crash login).
            return false;
        }
    }

    public bool NeedsRehash(string hash)
    {
        if (string.IsNullOrEmpty(hash)) return true;

        try
        {
            return BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, WorkFactor);
        }
        catch (SaltParseException)
        {
            return true; // can't parse → re-hash on next login
        }
    }
}
