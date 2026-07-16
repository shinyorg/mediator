using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// The shared <see cref="IExceptionHandler"/> chain. Every mediator entry point that can swallow or
/// surface an exception routes through here, so background publishing behaves the same as an awaited one.
/// </summary>
static class MediatorExceptionHandling
{
    /// <summary>
    /// Offers <paramref name="exception"/> to each registered <see cref="IExceptionHandler"/> in order,
    /// stopping at the first that returns true. Always records the exception on the context.
    /// </summary>
    /// <returns>True if a handler took ownership of the exception.</returns>
    public static async Task<bool> TryHandle(
        MediatorContext context,
        Exception exception,
        ILogger logger
    )
    {
        context.Exception = exception;

        if (context.BypassExceptionHandlingEnabled)
        {
            logger.LogDebug("Bypassing exception handling is enabled");
            return false;
        }

        var handled = false;
        using (context.StartActivity("Starting Exception Handling"))
        {
            var exceptionHandlers = context
                .ServiceScope
                .ServiceProvider
                .GetServices<IExceptionHandler>();

            foreach (var eh in exceptionHandlers)
            {
                var handlerType = eh.GetType().FullName ?? "Unknown";
                logger.LogDebug("Trying to handle exception with {HandlerType}", handlerType);

                handled = await eh
                    .Handle(
                        context,
                        exception
                    )
                    .ConfigureAwait(false);

                if (handled)
                {
                    logger.LogWarning(exception, "Exception handled by {HandlerType}", handlerType);
                    break;
                }
            }
        }

        if (!handled)
        {
            // we log as debug to let the exception bubble all the way out for the final app layers to decide the fate
            logger.LogDebug(exception, "No exception handlers managed the exception");
        }

        return handled;
    }
}
