using System.Net;

namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Ambient HTTP request metadata that handlers need for audit / security logs.
/// Implemented in Api layer over <c>IHttpContextAccessor</c>.
/// </summary>
public interface IClientContext
{
    IPAddress? IpAddress { get; }
    string UserAgent { get; }

    /// <summary>From <c>X-Device-Id</c> request header — present on mobile calls.</summary>
    string? DeviceFingerprint { get; }

    /// <summary>From <c>X-App-Version</c> request header.</summary>
    string? AppVersion { get; }

    /// <summary>From <c>Accept-Language</c> header (or current user pref); <c>vi</c> default.</summary>
    string Language { get; }
}
