using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Sentry;

namespace Shiny.Mediator;


/// <summary>
/// Registration extensions for the Sentry integration with Shiny Mediator.
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Registers Sentry-backed exception handling and middleware that emit transactions and spans
    /// around every request, command, event, and stream dispatch. Call <c>UseSentry</c> on your
    /// host builder separately to initialize the Sentry SDK itself.
    /// </summary>
    /// <param name="configurator">The mediator builder to wire into.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ShinyMediatorBuilder UseSentry(this ShinyMediatorBuilder configurator)
    {
        configurator.AddExceptionHandler<SentryExceptionHandler>();
        configurator.AddOpenEventMiddleware(typeof(SentryEventMiddleware<>), ServiceLifetime.Singleton);
        configurator.AddOpenCommandMiddleware(typeof(SentryCommandMiddleware<>), ServiceLifetime.Singleton);
        configurator.AddOpenRequestMiddleware(typeof(SentryRequestMiddleware<,>), ServiceLifetime.Singleton);
        configurator.AddOpenStreamMiddleware(typeof(SentryStreamRequestMiddleware<,>), ServiceLifetime.Singleton);
        return configurator;
    }
}
