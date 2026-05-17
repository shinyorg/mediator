using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Handlers;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="MauiAppBuilder"/> and <see cref="ShinyMediatorBuilder"/> extensions that register
/// Shiny Mediator services, MAUI-specific infrastructure (storage, connectivity, dialogs),
/// HTTP request decoration, Shell navigation, and the <see cref="MainThreadAttribute"/> middleware.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers Shiny Mediator on a .NET MAUI app builder. Internally calls <see cref="UseMaui"/>
    /// to install the MAUI event collector and, when <paramref name="includeStandardMiddleware"/>
    /// is <c>true</c>, the standard app-support middleware stack (exception handling, main thread,
    /// timed requests, offline availability).
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="configAction">Optional callback to register handlers and additional middleware.</param>
    /// <param name="includeStandardMiddleware">When <c>true</c> (default), installs the standard middleware stack.</param>
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
    /// Registers a file-based <see cref="StorageCacheService"/> backed by MAUI's
    /// <see cref="IFileSystem.CacheDirectory"/> so cached responses survive across app sessions.
    /// Also wires the MAUI infrastructure services required by the cache store.
    /// </summary>
    public static ShinyMediatorBuilder AddMauiPersistentCache(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddMauiInfrastructure();
        mediatorBuilder.AddCaching<StorageCacheService>();
        return mediatorBuilder;
    }

    
    /// <summary>
    /// Registers <see cref="ConnectivityBroadcaster"/> as an <see cref="IMauiInitializeService"/>
    /// so a <c>ConnectivityChanged</c> event is published through the mediator whenever the
    /// device's internet availability changes, and re-broadcast to handlers on the appearing
    /// page or its binding context.
    /// </summary>
    public static ShinyMediatorBuilder AddConnectivityBroadcaster(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddMauiInfrastructure();
        mediatorBuilder.Services.AddSingleton<IMauiInitializeService, ConnectivityBroadcaster>();
        return mediatorBuilder;
    }
    

    /// <summary>
    /// Registers the MAUI event collector (<see cref="MauiEventCollector"/>) and, when
    /// <paramref name="includeStandardMiddleware"/> is <c>true</c>, the MAUI infrastructure
    /// services along with main-thread and standard app-support middleware.
    /// </summary>
    /// <param name="cfg">The mediator builder.</param>
    /// <param name="includeStandardMiddleware">If <c>true</c>, event exception handling, main-thread event handling, timed requests, and offline availability middleware is installed.</param>
    public static ShinyMediatorBuilder UseMaui(this ShinyMediatorBuilder cfg, bool includeStandardMiddleware = true)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<MauiEventCollector>();

        if (includeStandardMiddleware)
        {
            cfg.AddMauiInfrastructure();
            cfg.AddMainThreadMiddleware();
            cfg.AddStandardAppSupportMiddleware();
        }
        return cfg;
    }


    /// <summary>
    /// Ensures all MAUI infrastructure services required by Shiny Mediator middleware are
    /// registered: <see cref="IStorageService"/>, <see cref="IInternetService"/>,
    /// <see cref="IAlertDialogService"/>, and MAUI Essentials singletons
    /// (<see cref="FileSystem"/>, <see cref="AppInfo"/>, <see cref="DeviceDisplay"/>,
    /// <see cref="DeviceInfo"/>, <see cref="Geolocation"/>, <see cref="Connectivity"/>).
    /// Uses <c>TryAdd</c> so existing registrations are preserved.
    /// </summary>
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
    /// Registers <see cref="MauiHttpRequestDecorator"/> which appends app identifier, app version,
    /// device manufacturer/model/platform/version, accept-language, and (when permitted) GPS
    /// coordinates to outbound HTTP requests issued through the mediator HTTP pipeline.
    /// </summary>
    public static ShinyMediatorBuilder AddMauiHttpDecorator(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddMauiInfrastructure();
        mediatorBuilder.Services.AddSingleton<IHttpRequestDecorator, MauiHttpRequestDecorator>();
        return mediatorBuilder;
    }

    
    /// <summary>
    /// Registers the open-generic <see cref="ShellNavigationCommandHandler{TCommand}"/> so any
    /// command implementing <see cref="IShellNavigationCommand"/> can be dispatched through the
    /// mediator to perform MAUI Shell navigation.
    /// </summary>
    public static ShinyMediatorBuilder AddShellNavigation(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.AddSingleton(typeof(ICommandHandler<>), typeof(ShellNavigationCommandHandler<>));
        return cfg;
    }


    /// <summary>
    /// Registers the event, request, and command middleware that observes the
    /// <see cref="MainThreadAttribute"/> on handlers and marshals their execution to the MAUI
    /// UI thread via <see cref="MainThread.BeginInvokeOnMainThread(Action)"/>.
    /// </summary>
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