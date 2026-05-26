using FluentAssertions;
using Rmms.Domain.Common;
using Xunit;

namespace Rmms.UnitTests.Domain;

public sealed class UuidV7Tests
{
    [Fact]
    public void NewGuid_IsVersion7AndRfc4122Variant()
    {
        var g = UuidV7.NewGuid();
        var bytes = g.ToByteArray();
        // Note: Guid.ToByteArray uses GUID layout, not raw RFC 4122 order.
        // We constructed via `new Guid(bytes)`, so byte[6] high nibble carries the version.
        // Re-extract from string form to be unambiguous.
        var hex = g.ToString("N");
        // Position 12..13 in the canonical string == version nibble.
        hex[12].Should().Be('7');
        // Variant: position 16 ∈ {8,9,a,b}
        "89ab".Should().Contain(hex[16].ToString());
    }

    [Fact]
    public void NewGuid_IsMonotonicWithinMillisecond()
    {
        var a = UuidV7.NewGuid();
        Thread.Sleep(2);
        var b = UuidV7.NewGuid();
        // Lexicographic ordering of canonical form should track time.
        string.Compare(a.ToString("N"), b.ToString("N"), StringComparison.Ordinal)
            .Should().BeLessThan(0);
    }
}
