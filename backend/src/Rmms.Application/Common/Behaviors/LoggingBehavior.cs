using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Rmms.Application.Common.Behaviors;

/// <summary>
/// Logs request name and elapsed milliseconds for every Mediator message.
/// Structured log property <c>RequestName</c> makes it easy to alert on slow handlers.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(message, cancellationToken);
            sw.Stop();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex,
                "Failed {RequestName} after {ElapsedMs}ms: {Message}",
                requestName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
