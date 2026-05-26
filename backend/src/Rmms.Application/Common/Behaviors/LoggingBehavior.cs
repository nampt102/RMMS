using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Rmms.Application.Common.Behaviors;

/// <summary>
/// Logs request name and elapsed milliseconds for every Mediator message.
/// Structured log property <c>RequestName</c> makes it easy to alert on slow handlers.
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TRequest, TResponse> next)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(message, cancellationToken);
            sw.Stop();
            LogHandled(_logger, requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogFailed(_logger, ex, requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMs}ms")]
    private static partial void LogHandled(ILogger logger, string requestName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed {RequestName} after {ElapsedMs}ms")]
    private static partial void LogFailed(ILogger logger, Exception ex, string requestName, long elapsedMs);
}
