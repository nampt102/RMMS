using Rmms.Application.Common.Interfaces;
using Serilog.Context;

namespace Rmms.Api.Logging;

/// <summary>
/// Pushes per-request properties (<c>TraceId</c>, <c>UserId</c>, <c>DeviceId</c>, <c>Role</c>) into
/// the Serilog <see cref="LogContext"/> so every log line emitted while handling the request carries
/// them (Serilog has <c>Enrich.FromLogContext()</c> enabled). Must run AFTER authentication so the
/// JWT claims are available. The request-completion log from <c>UseSerilogRequestLogging</c> is
/// enriched separately via <c>EnrichDiagnosticContext</c> (it sits above this middleware).
/// </summary>
public sealed class RequestEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public RequestEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, ICurrentUser currentUser)
    {
        var scopes = new List<IDisposable> { LogContext.PushProperty("TraceId", context.TraceIdentifier) };

        if (currentUser.UserId is { } userId)
        {
            scopes.Add(LogContext.PushProperty("UserId", userId));
        }

        if (currentUser.Role is { } role)
        {
            scopes.Add(LogContext.PushProperty("Role", role.ToString().ToLowerInvariant()));
        }

        if (currentUser.DeviceId is { } deviceId && deviceId != Guid.Empty)
        {
            scopes.Add(LogContext.PushProperty("DeviceId", deviceId));
        }

        try
        {
            await _next(context);
        }
        finally
        {
            for (var i = scopes.Count - 1; i >= 0; i--)
            {
                scopes[i].Dispose();
            }
        }
    }
}
