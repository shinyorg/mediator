using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Maui;
using Shiny.Mediator.Maui.Services;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MauiMediatorExtensions
{
    public static ShinyConfigurator UseMaui(this ShinyConfigurator cfg, bool includeStandardMiddleware = true)
    {
        cfg.AddEventCollector<MauiEventCollector>();
        
        if (includeStandardMiddleware)
        {
            cfg.AddEventExceptionHandlingMiddleware();
            cfg.AddMainThreadEventMiddleware();
            
            cfg.AddTimedMiddleware();
            cfg.AddCacheMiddleware();
            // cfg.AddUserNotificationExceptionMiddleware();
        }
        return cfg;
    }

    
    public static ShinyConfigurator AddTimedMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenRequestMiddleware(typeof(TimedLoggingRequestMiddleware<,>));


    public static ShinyConfigurator AddEventExceptionHandlingMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenEventMiddleware(typeof(ExceptionHandlerEventMiddleware<>));
    

    public static ShinyConfigurator AddMainThreadEventMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenEventMiddleware(typeof(MainTheadEventMiddleware<>));
    
    
    public static ShinyConfigurator AddCacheMiddleware(this ShinyConfigurator cfg)
    {
        cfg.Services.TryAddSingleton(Connectivity.Current);
        cfg.Services.TryAddSingleton(FileSystem.Current);
        cfg.Services.TryAddSingleton<CacheManager>();
        cfg.Services.AddSingletonAsImplementedInterfaces<CacheHandlers>();
        cfg.AddOpenRequestMiddleware(typeof(CacheRequestMiddleware<,>));
        return cfg;
    }
    
    
    public static ShinyConfigurator AddReplayStreamMiddleware(this ShinyConfigurator cfg)
    {
        cfg.Services.TryAddSingleton(Connectivity.Current);
        cfg.Services.TryAddSingleton(FileSystem.Current);
        cfg.AddOpenStreamMiddleware(typeof(ReplayStreamMiddleware<,>));
        return cfg;
    }
    
    
    public static ShinyConfigurator AddUserNotificationExceptionMiddleware(this ShinyConfigurator cfg, UserExceptionRequestMiddlewareConfig config)
    {
        cfg.Services.AddSingleton(config);
        cfg.AddOpenRequestMiddleware(typeof(UserExceptionRequestMiddleware<,>));
        return cfg;
    }
}