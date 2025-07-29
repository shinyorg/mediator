namespace Shiny.Mediator;


public static class MediatorExtensions
{
    /// <summary>
    /// Wires up Sentry to Mediator middleware - be sure to call UseSentry on your hosting provider
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder UseSentry(this ShinyMediatorBuilder configurator)
    {
        // configurator.AddExceptionHandler<SentryExceptionHandler>();
        // configurator.AddOpenEventMiddleware(typeof(SentryEventMiddleware<>), ServiceLifetime.Singleton);
        // configurator.AddOpenCommandMiddleware(typeof(SentryCommandMiddleware<>), ServiceLifetime.Singleton);
        // configurator.AddOpenRequestMiddleware(typeof(SentryRequestMiddleware<,>), ServiceLifetime.Singleton);
        // configurator.AddOpenStreamMiddleware(typeof(SentryStreamRequestMiddleware<,>), ServiceLifetime.Singleton);
        return configurator;
    }  
}