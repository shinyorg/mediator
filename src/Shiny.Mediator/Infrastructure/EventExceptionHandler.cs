using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Built-in <see cref="IExceptionHandler"/> registered by <c>PreventEventExceptions</c>. Logs and swallows
/// exceptions thrown while processing an <see cref="IEvent"/>, leaving other contracts unaffected.
/// </summary>
public class EventExceptionHandler(ILogger<EventExceptionHandler>? logger = null) : IExceptionHandler
{
    /// <inheritdoc/>
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        var handled = false;
        if (context.Message is IEvent)
        {
            logger?.LogError(exception, "Error occurred in event handler");
            handled = true;
        }
        return Task.FromResult(handled);
    }
}