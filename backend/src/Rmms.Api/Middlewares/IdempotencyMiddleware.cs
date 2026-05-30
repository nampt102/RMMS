using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rmms.Application.Common.Interfaces;
using Rmms.Shared.Errors;
using StackExchange.Redis;

namespace Rmms.Api.Middlewares;

/// <summary>
/// Implements <c>X-Idempotency-Key</c> handling per <c>05-api-conventions.md</c>:
/// for mutating requests carrying the header, the first successful response is cached in
/// Redis for 24h and replayed verbatim on any re-request with the same key. A concurrent
/// duplicate that arrives while the first request is still in flight gets
/// <c>409 IDEMPOTENCY_KEY_REUSED</c>.
///
/// Scope is (userId + method + path + key) so the same key on a different endpoint or user
/// never collides. Fails open: if Redis is unavailable the request proceeds without caching.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "X-Idempotency-Key";
    private const string ReplayHeader = "Idempotency-Replayed";
    private static readonly TimeSpan ResponseTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        // Only mutations carrying the header participate.
        if (!MutatingMethods.Contains(context.Request.Method) ||
            !context.Request.Headers.TryGetValue(HeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues))
        {
            await _next(context);
            return;
        }

        var scope = BuildScope(context, currentUser, keyValues.ToString());
        var respKey = $"idem:resp:{scope}";
        var lockKey = $"idem:lock:{scope}";

        IDatabase db;
        try
        {
            db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            // Redis unreachable -> fail open, do not block the request.
            _logger.LogWarning(ex, "Idempotency: Redis unavailable, proceeding without caching.");
            await _next(context);
            return;
        }

        // 1) Replay a previously cached response if present.
        var cached = await SafeStringGet(db, respKey);
        if (cached is not null)
        {
            await WriteCachedAsync(context, cached);
            return;
        }

        // 2) Acquire an in-flight lock; a concurrent duplicate is rejected.
        var gotLock = await SafeLock(db, lockKey);
        if (!gotLock)
        {
            // The first request may have finished between our GET and lock attempt — re-check.
            cached = await SafeStringGet(db, respKey);
            if (cached is not null)
            {
                await WriteCachedAsync(context, cached);
                return;
            }

            await WriteReusedAsync(context);
            return;
        }

        // 3) Run the pipeline while buffering the response body.
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;
            var bodyBytes = buffer.ToArray();

            // Only cache successful responses; let clients retry failures under the same key.
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var snapshot = new CachedResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType,
                    Convert.ToBase64String(bodyBytes));
                await SafeStore(db, respKey, JsonSerializer.Serialize(snapshot, JsonOpts));
            }

            // Stream the real response back to the client.
            context.Response.Body = originalBody;
            await context.Response.Body.WriteAsync(bodyBytes);
        }
        finally
        {
            context.Response.Body = originalBody;
            await SafeRelease(db, lockKey);
        }
    }

    private static string BuildScope(HttpContext ctx, ICurrentUser user, string key)
    {
        var who = user.UserId?.ToString() ?? "anon";
        var raw = $"{who}|{ctx.Request.Method}|{ctx.Request.Path}|{key}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static async Task WriteCachedAsync(HttpContext ctx, string cachedJson)
    {
        var snapshot = JsonSerializer.Deserialize<CachedResponse>(cachedJson, JsonOpts)!;
        ctx.Response.Clear();
        ctx.Response.StatusCode = snapshot.StatusCode;
        ctx.Response.ContentType = snapshot.ContentType ?? "application/json; charset=utf-8";
        ctx.Response.Headers[ReplayHeader] = "true";
        await ctx.Response.Body.WriteAsync(Convert.FromBase64String(snapshot.BodyBase64));
    }

    private static async Task WriteReusedAsync(HttpContext ctx)
    {
        ctx.Response.Clear();
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        var body = new ErrorEnvelope(new ErrorBody(
            ErrorCodes.IdempotencyKeyReused,
            "Yêu cầu trùng lặp đang được xử lý.",
            null,
            ctx.TraceIdentifier));
        await JsonSerializer.SerializeAsync(ctx.Response.Body, body, JsonOpts);
    }

    private async Task<string?> SafeStringGet(IDatabase db, string key)
    {
        try
        {
            var v = await db.StringGetAsync(key);
            return v.HasValue ? v.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency: Redis GET failed for {Key}.", key);
            return null;
        }
    }

    private async Task<bool> SafeLock(IDatabase db, string key)
    {
        try
        {
            return await db.StringSetAsync(key, "1", LockTtl, When.NotExists);
        }
        catch (Exception ex)
        {
            // Fail open: if we cannot lock, allow the request through (no dedupe this time).
            _logger.LogWarning(ex, "Idempotency: Redis lock failed for {Key}.", key);
            return true;
        }
    }

    private async Task SafeStore(IDatabase db, string key, string value)
    {
        try
        {
            await db.StringSetAsync(key, value, ResponseTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency: Redis store failed for {Key}.", key);
        }
    }

    private async Task SafeRelease(IDatabase db, string key)
    {
        try
        {
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency: Redis lock release failed for {Key}.", key);
        }
    }

    private sealed record CachedResponse(int StatusCode, string? ContentType, string BodyBase64);
}
