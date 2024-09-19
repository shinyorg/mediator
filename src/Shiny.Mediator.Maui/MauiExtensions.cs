using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Handlers;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MauiExtensions
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
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton(FileSystem.Current);
        cfg.Services.TryAddSingleton(Connectivity.Current);

        
        if (includeStandardMiddleware)
        {
            cfg.AddMainThreadMiddleware();
            cfg.AddUserNotificationExceptionMiddleware();
            cfg.AddOfflineAvailabilityMiddleware();
            cfg.AddReplayStreamMiddleware();
        }
        return cfg;
    }


    /// <summary>
    /// This appends app version, device info, and culture to the HTTP request handling framework
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="appendGpsHeader"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddMauiHttpDecorator(this ShinyConfigurator configurator, bool appendGpsHeader = false)
    {
        configurator.Services.TryAddSingleton(AppInfo.Current);
        configurator.Services.TryAddSingleton(DeviceDisplay.Current);
        configurator.Services.TryAddSingleton(DeviceInfo.Current);
        configurator.Services.TryAddSingleton(Geolocation.Default);
        
        configurator.Services.Add(new ServiceDescriptor(
            typeof(IHttpRequestDecorator<,>), 
            null,
            typeof(MauiHttpRequestDecorator<,>),
            ServiceLifetime.Singleton
        ));
        return configurator;
    }

    
    /// <summary>
    /// Add Strongly Typed Shell Navigator
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddShellNavigation(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingleton(typeof(IRequestHandler<>), typeof(ShellNavigationRequestHandler<>));
        return cfg;
    }


    /// <summary>
    /// Allows for [MainThread] marking on Request & Event Handlers
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddMainThreadMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddOpenEventMiddleware(typeof(MainTheadEventMiddleware<>));
        cfg.AddOpenRequestMiddleware(typeof(MainThreadRequestHandler<,>));
        
        return cfg;
    }


    /// <summary>
    /// Allows you to mark [UserNotify] on your request handlers which logs an error & displays an alert to the user
    /// to show a customized message
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddUserNotificationExceptionMiddleware(this ShinyConfigurator cfg, UserExceptionRequestMiddlewareConfig? config = null)
    {
        cfg.Services.AddSingleton(config ?? new());
        cfg.AddOpenRequestMiddleware(typeof(UserExceptionRequestMiddleware<,>));
        return cfg;
    }
}