using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class AppSupportExtensions
{
    /// <summary>
    /// Adds standard app support middleware - offline, replay stream, & user notification
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddStandardAppSupportMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddUserNotificationExceptionMiddleware();
        cfg.AddOfflineAvailabilityMiddleware();
        cfg.AddReplayStreamMiddleware();
        return cfg;
    }
    
    
    /// <summary>
    /// Allows you to mark [UserNotify] on your request handlers which logs an error & displays an alert to the user
    /// to show a customized message
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddUserNotificationExceptionMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddOpenRequestMiddleware(typeof(UserExceptionRequestMiddleware<,>));
        return cfg;
    }
    
    
    /// <summary>
    /// Allows your request /w result handlers to return a stored value when offline is detected
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddOfflineAvailabilityMiddleware(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<OfflineAvailableFlushRequestHandler>();
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