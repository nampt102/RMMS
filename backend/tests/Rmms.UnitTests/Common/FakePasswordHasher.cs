using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>
/// Trivially-fast (no BCrypt) hasher for tests — encodes <c>"plain:{value}"</c>.
/// BCrypt cost-12 is too slow to use in unit tests (≈200 ms each).
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string plaintext) => $"plain:{plaintext}";

    public bool Verify(string plaintext, string hash) =>
        hash == $"plain:{plaintext}";

    public bool NeedsRehash(string hash) => false;
}
