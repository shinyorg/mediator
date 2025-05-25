using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Handlers;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MauiAppBuilderExtensions
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

/*
#if __IOS__
               events.AddiOS(lifecycle =>
               {
                   lifecycle.FinishedLaunching((application, launchOptions) =>
                   {
                       // A bit of hackery here, because we can't mock UIKit.UIApplication in tests.
                       var platformApplication = application != null!
                           ? application.Delegate as IPlatformApplication
                           : launchOptions["application"] as IPlatformApplication;
   
                       platformApplication?.HandleMauiEvents();
                       return true;
                   });
                   lifecycle.WillTerminate(application =>
                   {
                       if (application == null!)
                       {
                           return;
                       }
   
                       var platformApplication = application.Delegate as IPlatformApplication;
                       platformApplication?.HandleMauiEvents(bind: false);
   
                       //According to https://developer.apple.com/documentation/uikit/uiapplicationdelegate/1623111-applicationwillterminate#discussion
                       //WillTerminate is called: in situations where the app is running in the background (not suspended) and the system needs to terminate it for some reason.
                       SentryMauiEventProcessor.InForeground = false;
                   });
   
                   lifecycle.OnActivated(application => SentryMauiEventProcessor.InForeground = true);
   
                   lifecycle.DidEnterBackground(application => SentryMauiEventProcessor.InForeground = false);
                   lifecycle.OnResignActivation(application => SentryMauiEventProcessor.InForeground = false);
               });
   #elif ANDROID
               events.AddAndroid(lifecycle =>
               {
                   lifecycle.OnApplicationCreating(application => (application as IPlatformApplication)?.HandleMauiEvents());
                   lifecycle.OnDestroy(application => (application as IPlatformApplication)?.HandleMauiEvents(bind: false));
   
                   lifecycle.OnResume(activity => SentryMauiEventProcessor.InForeground = true);
                   lifecycle.OnStart(activity => SentryMauiEventProcessor.InForeground = true);
   
                   lifecycle.OnStop(activity => SentryMauiEventProcessor.InForeground = false);
                   lifecycle.OnPause(activity => SentryMauiEventProcessor.InForeground = false);
               });
   #elif WINDOWS
               events.AddWindows(lifecycle =>
               {
                   lifecycle.OnLaunching((application, _) => (application as IPlatformApplication)?.HandleMauiEvents());
                   lifecycle.OnClosed((application, _) => (application as IPlatformApplication)?.HandleMauiEvents(bind: false));
               });
   #endif
 */