using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Handlers;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MauiExtensions
{
    /// <summary>
    /// Easier path to add Shiny Mediator to Maui
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configAction"></param>
    /// <param name="includeStandardMiddleware"></param>
    /// <returns></returns>
    public static MauiAppBuilder AddShinyMediator(
        this MauiAppBuilder builder,
        Action<ShinyConfigurator>? configAction = null,
        bool includeStandardMiddleware = true
    )
    {
        builder.Services.AddShinyMediator(cfg =>
        {
            cfg.UseMaui(includeStandardMiddleware);
            configAction?.Invoke(cfg);
        });
        return builder;
    }


    /// <summary>
    /// Adds a file based caching service - ideal for cache surviving across app sessions
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddMauiPersistentCache(this ShinyConfigurator configurator)
    {
        configurator.AddMauiInfrastructure();
        configurator.AddCaching<StorageCacheService>();
        return configurator;
    }

    
    /// <summary>
    /// Adds connectivity broadcaster
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddConnectivityBroadcaster(this ShinyConfigurator configurator)
    {
        configurator.AddMauiInfrastructure();
        configurator.Services.AddSingleton<IMauiInitializeService, ConnectivityBroadcaster>();
        return configurator;
    }
    

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
            cfg.AddMauiInfrastructure();
            cfg.AddMainThreadMiddleware();
            cfg.AddStandardAppSupportMiddleware();
        }
        return cfg;
    }


    /// <summary>
    /// Ensures all necessary MAUI services are installed for middleware
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddMauiInfrastructure(this ShinyConfigurator cfg)
    {
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        cfg.Services.TryAddSingleton(FileSystem.Current);
        cfg.Services.TryAddSingleton(AppInfo.Current);
        cfg.Services.TryAddSingleton(DeviceDisplay.Current);
        cfg.Services.TryAddSingleton(DeviceInfo.Current);
        cfg.Services.TryAddSingleton(Geolocation.Default);
        cfg.Services.TryAddSingleton(Connectivity.Current);
        return cfg;
    }

    
    /// <summary>
    /// This appends app version, device info, and culture to the HTTP request handling framework
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddMauiHttpDecorator(this ShinyConfigurator configurator)
    {
        configurator.AddMauiInfrastructure();
        configurator.Services.AddSingleton(typeof(IHttpRequestDecorator<,>), typeof(MauiHttpRequestDecorator<,>));
        return configurator;
    }

    
    /// <summary>
    /// Add Strongly Typed Shell Navigator
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddShellNavigation(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(ShellNavigationCommandHandler<>));
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
        cfg.AddOpenRequestMiddleware(typeof(MainThreadRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(MainThreadCommandMiddleware<>));
        return cfg;
    }
}