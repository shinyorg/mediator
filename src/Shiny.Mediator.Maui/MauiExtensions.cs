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
        Action<ShinyMediatorBuilder>? configAction = null,
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
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddMauiPersistentCache(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddMauiInfrastructure();
        mediatorBuilder.AddCaching<StorageCacheService>();
        return mediatorBuilder;
    }

    
    /// <summary>
    /// Adds connectivity broadcaster
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddConnectivityBroadcaster(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddMauiInfrastructure();
        mediatorBuilder.Services.AddSingleton<IMauiInitializeService, ConnectivityBroadcaster>();
        return mediatorBuilder;
    }
    

    /// <summary>
    /// Adds Maui Event Collector to mediator
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="includeStandardMiddleware">If true, event exception handling, main thread event handling, timed requests, and offline availability middle is installed</param>
    /// <returns></returns>
    public static ShinyMediatorBuilder UseMaui(this ShinyMediatorBuilder cfg, bool includeStandardMiddleware = true)
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
    public static ShinyMediatorBuilder AddMauiInfrastructure(this ShinyMediatorBuilder cfg)
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
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddMauiHttpDecorator(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddMauiInfrastructure();
        mediatorBuilder.Services.AddSingleton(typeof(IHttpRequestDecorator<,>), typeof(MauiHttpRequestDecorator<,>));
        return mediatorBuilder;
    }

    
    /// <summary>
    /// Add Strongly Typed Shell Navigator
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddShellNavigation(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(ShellNavigationCommandHandler<>));
        return cfg;
    }


    /// <summary>
    /// Allows for [MainThread] marking on Request & Event Handlers
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddMainThreadMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.AddOpenEventMiddleware(typeof(MainTheadEventMiddleware<>));
        cfg.AddOpenRequestMiddleware(typeof(MainThreadRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(MainThreadCommandMiddleware<>));
        return cfg;
    }
}