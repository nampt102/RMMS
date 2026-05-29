using System.Net;
using System.Net.Sockets;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Api.Authentication;

/// <summary>
/// Resolves ambient HTTP request metadata for audit / security purposes.
/// Implementation of <see cref="IClientContext"/> over <see cref="IHttpContextAccessor"/>.
/// </summary>
public sealed class HttpContextClientContext : IClientContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextClientContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public IPAddress? IpAddress
    {
        get
        {
            var ctx = _accessor.HttpContext;
            if (ctx is null) return null;

            // Respect X-Forwarded-For when behind Caddy (per ADR-007). Configured via
            // ForwardedHeadersMiddleware in Program.cs to populate Connection.RemoteIpAddress.
            var addr = ctx.Connection.RemoteIpAddress;
            if (addr is null) return null;

            // Normalize IPv4-mapped IPv6 (::ffff:1.2.3.4) → 1.2.3.4 for cleaner logs.
            if (addr.IsIPv4MappedToIPv6)
            {
                addr = addr.MapToIPv4();
            }
            return addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6
                ? addr
                : null;
        }
    }

    public string UserAgent =>
        _accessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

    public string? DeviceFingerprint =>
        _accessor.HttpContext?.Request.Headers["X-Device-Id"].FirstOrDefault();

    public string? AppVersion =>
        _accessor.HttpContext?.Request.Headers["X-App-Version"].FirstOrDefault();

    public string Language
    {
        get
        {
            var headerLang = _accessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
            if (string.IsNullOrWhiteSpace(headerLang)) return "vi";

            // Take the first language tag, lower-case, normalize to vi/en.
            var primary = headerLang.Split(',', ';')[0].Trim().ToLowerInvariant();
            return primary.StartsWith("en", StringComparison.Ordinal) ? "en" : "vi";
        }
    }
}
