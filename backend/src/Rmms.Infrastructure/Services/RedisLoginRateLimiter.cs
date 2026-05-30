using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;
using StackExchange.Redis;

namespace Rmms.Infrastructure.Services;

/// <summary>
/// Redis-backed implementation of <see cref="ILoginRateLimiter"/>.
/// Counts failed logins per (email + IP) in a 15-minute window; 5 failures block.
/// Fails open if Redis is unavailable.
/// </summary>
public sealed class RedisLoginRateLimiter : ILoginRateLimiter
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisLoginRateLimiter> _logger;

    public RedisLoginRateLimiter(IConnectionMultiplexer redis, ILogger<RedisLoginRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> IsBlockedAsync(string email, string ipAddress, CancellationToken ct = default)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(Key(email, ipAddress));
            return value.HasValue && (long)value >= MaxFailures;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoginRateLimiter: Redis read failed, allowing attempt (fail-open).");
            return false;
        }
    }

    public async Task RegisterFailureAsync(string email, string ipAddress, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = Key(email, ipAddress);
            var count = await db.StringIncrementAsync(key);
            if (count == 1)
            {
                // First failure in this window — set the TTL.
                await db.KeyExpireAsync(key, Window);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoginRateLimiter: Redis increment failed (fail-open).");
        }
    }

    public async Task ResetAsync(string email, string ipAddress, CancellationToken ct = default)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(Key(email, ipAddress));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoginRateLimiter: Redis reset failed.");
        }
    }

    private static string Key(string email, string ipAddress)
    {
        var raw = $"{email.Trim().ToLowerInvariant()}|{ipAddress}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"login:fail:{Convert.ToHexString(hash)}";
    }
}
