using System.Security.Cryptography;

namespace Rmms.Domain.Common;

/// <summary>
/// App-side UUID v7 generator (RFC 9562).
/// Time-ordered → good for PostgreSQL B-tree index locality.
/// Per <c>knowledge-base/08-coding-standards.md</c>: "uuid PK with uuid_generate_v7() (need extension or app-generated)".
/// </summary>
public static class UuidV7
{
    /// <summary>Generates a new UUID v7.</summary>
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        // 48-bit Unix timestamp in milliseconds (big-endian, first 6 bytes)
        var unixMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(unixMs >> 40);
        bytes[1] = (byte)(unixMs >> 32);
        bytes[2] = (byte)(unixMs >> 24);
        bytes[3] = (byte)(unixMs >> 16);
        bytes[4] = (byte)(unixMs >> 8);
        bytes[5] = (byte)unixMs;

        // Version 7: top 4 bits of byte[6] = 0111
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        // Variant RFC 4122: top 2 bits of byte[8] = 10
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        // IMPORTANT: must use bigEndian: true. The legacy `new Guid(byte[])`
        // constructor interprets bytes 0-3, 4-5, 6-7 as LITTLE-endian fields,
        // which would swap byte[6] (our version nibble) and byte[7], making
        // the canonical string form non-compliant with RFC 9562.
        // The bigEndian overload (added in .NET 8) reads the buffer exactly
        // as RFC 4122 / 9562 specify.
        return new Guid(bytes, bigEndian: true);
    }
}
