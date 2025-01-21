using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class AppSupportExtensions
{
    /// <summary>
    /// Adds a file based caching service - ideal for cache surviving across app sessions
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddPersistentCache(this ShinyConfigurator configurator)
        => configurator.AddCaching<StorageCacheService>();
    
    
    /// <summary>
    /// Adds standard app support middleware - offline, replay stream, & user notification
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddStandardAppSupportMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddUserErrorNotificationsMiddleware();
        cfg.AddOfflineAvailabilityMiddleware();
        cfg.AddReplayStreamMiddleware();
        return cfg;
    }
    
    
    /// <summary>
    /// Allows you to configure error handling on your request handlers which logs an error & displays an alert to the user
    /// to show a customized message
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddUserErrorNotificationsMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddOpenRequestMiddleware(typeof(UserErrorNotificationsRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(UserErrorNotificationsCommandMiddleware<>));
        return cfg;
    }
    
    
    /// <summary>
    /// Allows your request /w result handlers to return a stored value when offline is detected
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddOfflineAvailabilityMiddleware(this ShinyConfigurator cfg)
    {
        cfg.Services.TryAddSingleton<IOfflineService, OfflineService>();
        cfg.Services.AddSingletonAsImplementedInterfaces<OfflineFlushEventHandlers>();
        cfg.AddOpenRequestMiddleware(typeof(OfflineAvailableRequestMiddleware<,>));
        
        return cfg;
    }
    
    
    /// <summary>
    /// Plays the last value for the request while requesting the next value
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddReplayStreamMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddOpenStreamMiddleware(typeof(ReplayStreamMiddleware<,>));
        return cfg;
    }
}