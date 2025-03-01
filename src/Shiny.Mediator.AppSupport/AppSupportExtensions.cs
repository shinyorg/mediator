using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class AppSupportExtensions
{
    /// <summary>
    /// Adds standard app support middleware - offline, replay stream, & user notification
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddStandardAppSupportMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.AddUserErrorNotificationsHandling();
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
    public static ShinyMediatorBuilder AddUserErrorNotificationsHandling(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.AddScoped<IExceptionHandler, UserNotificationExceptionHandler>();
        return cfg;
    }
    
    
    /// <summary>
    /// Allows your request /w result handlers to return a stored value when offline is detected
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddOfflineAvailabilityMiddleware(this ShinyMediatorBuilder cfg)
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
    public static ShinyMediatorBuilder AddReplayStreamMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.AddOpenStreamMiddleware(typeof(ReplayStreamMiddleware<,>));
        return cfg;
    }
}