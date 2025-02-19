using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

public static class SentryExtensions
{
    /// <summary>
    /// Wires up Sentry to Mediator middleware - be sure to call UseSentry on your hosting provider
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator UseSentry(this ShinyConfigurator configurator)
    {
        configurator.AddExceptionHandler<SentryExceptionHandler>();
        configurator.AddOpenEventMiddleware(typeof(SentryEventMiddleware<>), ServiceLifetime.Singleton);
        configurator.AddOpenCommandMiddleware(typeof(SentryCommandMiddleware<>), ServiceLifetime.Singleton);
        configurator.AddOpenRequestMiddleware(typeof(SentryRequestMiddleware<,>), ServiceLifetime.Singleton);
        configurator.AddOpenStreamMiddleware(typeof(SentryStreamRequestMiddleware<,>), ServiceLifetime.Singleton);
        return configurator;
    }
}