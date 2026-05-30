namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Throttles repeated failed login attempts per (email + client IP) to mitigate
/// credential-stuffing / brute force. Policy per sprint-01 Day 5:
/// 5 failures within a 15-minute sliding window blocks further attempts.
///
/// Backed by Redis so the limit holds across API instances. Implementations must
/// fail open (allow the request) if the backing store is unavailable — availability
/// of login is more important than perfect throttling.
/// </summary>
public interface ILoginRateLimiter
{
    /// <summary>True if this (email, ip) pair has exceeded the failure threshold.</summary>
    Task<bool> IsBlockedAsync(string email, string ipAddress, CancellationToken ct = default);

    /// <summary>Record one failed login attempt and start/extend the window.</summary>
    Task RegisterFailureAsync(string email, string ipAddress, CancellationToken ct = default);

    /// <summary>Clear the failure counter after a successful login.</summary>
    Task ResetAsync(string email, string ipAddress, CancellationToken ct = default);
}
