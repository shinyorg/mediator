using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.OpenTelemetry;

namespace Shiny.Mediator;

public static class MediatorExtensions
{
    /// <summary>
    /// Wires up OpenTelemetry to Mediator middleware - be sure to configure OpenTelemetry on your hosting provider
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder UseOpenTelemetry(this ShinyMediatorBuilder configurator)
    {
        configurator.AddExceptionHandler<OpenTelemetryExceptionHandler>();
        configurator.AddOpenEventMiddleware(typeof(OpenTelemetryEventMiddleware<>), ServiceLifetime.Singleton);
        configurator.AddOpenCommandMiddleware(typeof(OpenTelemetryCommandMiddleware<>), ServiceLifetime.Singleton);
        configurator.AddOpenRequestMiddleware(typeof(OpenTelemetryRequestMiddleware<,>), ServiceLifetime.Singleton);
        configurator.AddOpenStreamMiddleware(typeof(OpenTelemetryStreamRequestMiddleware<,>), ServiceLifetime.Singleton);
        return configurator;
    }  
}