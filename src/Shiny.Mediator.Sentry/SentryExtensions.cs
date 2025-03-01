using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

public static class SentryExtensions
{
    /// <summary>
    /// Wires up Sentry to Mediator middleware - be sure to call UseSentry on your hosting provider
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder UseSentry(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddExceptionHandler<SentryExceptionHandler>();
        mediatorBuilder.AddOpenEventMiddleware(typeof(SentryEventMiddleware<>), ServiceLifetime.Singleton);
        mediatorBuilder.AddOpenCommandMiddleware(typeof(SentryCommandMiddleware<>), ServiceLifetime.Singleton);
        mediatorBuilder.AddOpenRequestMiddleware(typeof(SentryRequestMiddleware<,>), ServiceLifetime.Singleton);
        mediatorBuilder.AddOpenStreamMiddleware(typeof(SentryStreamRequestMiddleware<,>), ServiceLifetime.Singleton);
        return mediatorBuilder;
    }
}