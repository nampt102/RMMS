using System.Net;
using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>Stub <see cref="IClientContext"/> with deterministic test values.</summary>
internal sealed class TestClientContext : IClientContext
{
    public IPAddress? IpAddress { get; init; } = IPAddress.Loopback;
    public string UserAgent { get; init; } = "RmmsUnitTest/1.0";
    public string? DeviceFingerprint { get; init; }
    public string? AppVersion { get; init; } = "1.0.0";
    public string Language { get; init; } = "vi";
}
