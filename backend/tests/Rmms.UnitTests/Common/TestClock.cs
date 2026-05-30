using Rmms.Application.Common.Interfaces;

namespace Rmms.UnitTests.Common;

/// <summary>Deterministic time provider for tests — caller controls every <c>UtcNow</c> read.</summary>
internal sealed class TestClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 06, 01, 09, 00, 00, TimeSpan.Zero);

    public void Advance(TimeSpan delta) => UtcNow += delta;
}
