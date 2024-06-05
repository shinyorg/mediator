using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;

public static class MauiMediatorExtensions
{
    public static ShinyConfigurator UseMaui(this ShinyConfigurator cfg) => cfg.AddEventCollector<MauiEventCollector>();

    public static ShinyConfigurator AddTimedMiddleware(this ShinyConfigurator cfg, TimedLoggingMiddlewareConfig config)
    {
        cfg.Services.AddSingleton(config);
        return cfg.AddOpenRequestMiddleware(typeof(TimedLoggingRequestMiddleware<,>));
    }



    public static ShinyConfigurator AddConnectivityCacheMiddleware(this ShinyConfigurator cfg)
    {
        // TODO: how to clear memory
        cfg.AddOpenRequestMiddleware(typeof(CacheRequestMiddleware<,>));
        return cfg;
    }
    
    
    public static ShinyConfigurator AddUserNotificationExceptionMiddleware(this ShinyConfigurator cfg, UserExceptionRequestMiddlewareConfig config)
    {
        cfg.Services.AddSingleton(config);
        cfg.AddOpenRequestMiddleware(typeof(UserExceptionRequestMiddleware<,>));
        return cfg;
    }
}