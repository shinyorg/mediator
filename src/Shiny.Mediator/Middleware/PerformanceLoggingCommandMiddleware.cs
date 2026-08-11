using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Times each command execution and logs a warning when it exceeds the configured threshold. The threshold
/// is read from <c>Mediator:PerformanceLogging</c> configuration; the breach duration is also recorded on the context.
/// </summary>
[MiddlewareOrder(1)]
public class PerformanceLoggingCommandMiddleware<TCommand>(
    IConfiguration? configuration = null,
    ILogger<TCommand>? logger = null
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    public async Task Process(
        IMediatorContext context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var section = configuration.GetHandlerSection("PerformanceLogging", context.Message!, context.MessageHandler);
        if (section == null)
        {
            await next().ConfigureAwait(false);
            return;
        }

        var millis = section.GetValue("ErrorThresholdMilliseconds", 5000);
        var ts = TimeSpan.FromMilliseconds(millis);

        var startTime = Stopwatch.GetTimestamp();
        await next();
        var delta = Stopwatch.GetElapsedTime(startTime);
        
        if (delta > ts)
        {
            context.SetPerformanceLoggingThresholdBreached(delta);
            logger?.LogError(
                "{CommandType} took longer than {Threshold} to execute - {Elapsed}", 
                typeof(TCommand), 
                ts,
                delta
            );
        }
        else if (logger?.IsEnabled(LogLevel.Debug) == true)
        {
            logger.LogDebug(
                "{CommandType} took {Elapsed} to execute",
                typeof(TCommand),
                delta
            );
        }
    }
}