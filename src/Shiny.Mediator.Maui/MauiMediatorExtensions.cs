using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Maui;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MauiMediatorExtensions
{
    /// <summary>
    /// Adds Maui Event Collector to mediator
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="includeStandardMiddleware">If true, event exception handling, main thread event handling, timed requests, and offline availability middle is installed</param>
    /// <returns></returns>
    public static ShinyConfigurator UseMaui(this ShinyConfigurator cfg, bool includeStandardMiddleware = true)
    {
        cfg.AddEventCollector<MauiEventCollector>();
        
        if (includeStandardMiddleware)
        {
            cfg.AddEventExceptionHandlingMiddleware();
            cfg.AddMainThreadEventMiddleware();
            
            cfg.AddTimedMiddleware();
            cfg.AddOfflineAvailabilityMiddleware();
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
    
    
    public static ShinyConfigurator AddOfflineAvailabilityMiddleware(this ShinyConfigurator cfg)
    {
        cfg.Services.TryAddSingleton(Connectivity.Current);
        cfg.Services.TryAddSingleton(FileSystem.Current);
        cfg.Services.AddSingletonAsImplementedInterfaces<OfflineAvailableFlushRequestHandler>();
        cfg.AddOpenRequestMiddleware(typeof(OfflineAvailableRequestMiddleware<,>));
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