using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Shiny.Mediator.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="IApplicationBuilder"/>, <see cref="IHostBuilder"/> and <see cref="ShinyMediatorBuilder"/>
/// extensions that register Shiny Mediator in Uno Platform apps along with the Uno infrastructure
/// (view event collection, main thread dispatch, storage, network information, dialogs) and the
/// standard app-support middleware.
/// </summary>
public static class UnoExtensions
{
    /// <summary>
    /// Registers Shiny Mediator on the Uno <see cref="IApplicationBuilder"/> and tracks
    /// <see cref="IApplicationBuilder.Window"/> so views and view models receive events, dialogs
    /// can be shown, and <see cref="MainThreadAttribute"/> handlers run on the UI thread.
    /// This is the recommended setup for Uno apps using Uno.Extensions hosting.
    /// </summary>
    /// <param name="builder">The Uno application builder from <c>this.CreateBuilder(args)</c>.</param>
    /// <param name="configure">Optional callback to register handlers and additional middleware.</param>
    /// <param name="includeStandardMiddleware">When <c>true</c> (default), installs the standard middleware stack.</param>
    public static IApplicationBuilder AddShinyMediator(
        this IApplicationBuilder builder,
        Action<ShinyMediatorBuilder>? configure = null,
        bool includeStandardMiddleware = true
    )
    {
        builder.Configure((host, window) => host.ConfigureServices(services => services.AddShinyMediator(
            cfg =>
            {
                cfg.UseUno(window, includeStandardMiddleware);
                configure?.Invoke(cfg);
            },
            includeStandardMiddleware
        )));
        return builder;
    }


    /// <summary>
    /// Registers Shiny Mediator on an Uno <see cref="IHostBuilder"/>. No window is tracked, so
    /// view event collection, dialogs, and <see cref="MainThreadAttribute"/> need the window added
    /// through <see cref="UnoWindowTracker.Add"/>. Prefer the <see cref="IApplicationBuilder"/> overload.
    /// </summary>
    /// <param name="builder">The Uno host builder.</param>
    /// <param name="configure">Optional callback to register handlers and additional middleware.</param>
    /// <param name="includeStandardMiddleware">When <c>true</c> (default), installs the standard middleware stack.</param>
    public static IHostBuilder AddShinyMediator(
        this IHostBuilder builder,
        Action<ShinyMediatorBuilder>? configure = null,
        bool includeStandardMiddleware = true
    )
    {
        builder.ConfigureServices(x => x.AddShinyMediator(
            cfg =>
            {
                cfg.UseUno(includeStandardMiddleware);
                configure?.Invoke(cfg);
            },
            includeStandardMiddleware
        ));
        return builder;
    }


    /// <summary>
    /// Registers the Uno event collector and window tracker and, when
    /// <paramref name="includeStandardMiddleware"/> is <c>true</c>, the Uno infrastructure services
    /// along with the main thread and standard app-support middleware. Pass your <see cref="Window"/>
    /// with <see cref="UseUno(ShinyMediatorBuilder, Window, bool)"/> so views can receive events.
    /// </summary>
    /// <param name="cfg">The mediator builder.</param>
    /// <param name="includeStandardMiddleware">If <c>true</c>, main thread, user error notification, offline availability, and replay stream middleware is installed.</param>
    public static ShinyMediatorBuilder UseUno(this ShinyMediatorBuilder cfg, bool includeStandardMiddleware = true)
    {
        cfg.Services.TryAddSingleton<UnoWindowTracker>();
        if (!cfg.Services.Any(x => x.ImplementationType == typeof(UnoEventCollector)))
            cfg.Services.AddSingletonAsImplementedInterfaces<UnoEventCollector>();

        if (includeStandardMiddleware)
        {
            cfg.AddUnoInfrastructure();
            cfg.AddMainThreadMiddleware<UnoMainThreadDispatcher>();
            cfg.AddStandardAppSupportMiddleware();
        }
        return cfg;
    }


    /// <summary>
    /// Same as <see cref="UseUno(ShinyMediatorBuilder, bool)"/>, and tracks <paramref name="window"/>
    /// so its views and view models receive events, dialogs show on it, and
    /// <see cref="MainThreadAttribute"/> handlers run on its UI thread.
    /// </summary>
    /// <param name="cfg">The mediator builder.</param>
    /// <param name="window">The app's main window.</param>
    /// <param name="includeStandardMiddleware">If <c>true</c>, main thread, user error notification, offline availability, and replay stream middleware is installed.</param>
    public static ShinyMediatorBuilder UseUno(this ShinyMediatorBuilder cfg, Window window, bool includeStandardMiddleware = true)
    {
        cfg.UseUno(includeStandardMiddleware);
        cfg.TrackUnoWindow(window);
        return cfg;
    }


    /// <summary>
    /// Registers a <see cref="StorageCacheService"/> backed by the Uno file-based storage service
    /// so cached responses survive across app sessions. Also wires the Uno infrastructure
    /// services required by the cache store.
    /// </summary>
    public static ShinyMediatorBuilder AddUnoPersistentCache(this ShinyMediatorBuilder cfg)
    {
        cfg.AddUnoInfrastructure();
        cfg.AddCaching<StorageCacheService>();
        return cfg;
    }


    /// <summary>
    /// Registers <see cref="ConnectivityBroadcaster"/> as an Uno <see cref="IServiceInitialize"/> so a
    /// <see cref="ConnectivityChanged"/> event is published through the mediator whenever the
    /// device's internet availability changes. Requires Uno.Extensions hosting.
    /// </summary>
    public static ShinyMediatorBuilder AddConnectivityBroadcaster(this ShinyMediatorBuilder cfg)
    {
        cfg.AddUnoInfrastructure();
        cfg.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceInitialize, ConnectivityBroadcaster>());
        return cfg;
    }


    /// <summary>
    /// Ensures the Uno infrastructure services required by Shiny Mediator middleware are
    /// registered: <see cref="UnoWindowTracker"/>, <see cref="IMainThreadDispatcher"/>,
    /// <see cref="IAlertDialogService"/> (<c>ContentDialog</c>), <see cref="IInternetService"/>
    /// (<c>NetworkInformation</c>), and <see cref="IStorageService"/> (<c>ApplicationData.LocalFolder</c>).
    /// Uses <c>TryAdd</c> so existing registrations are preserved.
    /// </summary>
    public static ShinyMediatorBuilder AddUnoInfrastructure(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.TryAddSingleton<UnoWindowTracker>();
        cfg.Services.TryAddSingleton<IMainThreadDispatcher, UnoMainThreadDispatcher>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        return cfg;
    }


    // the tracker is created lazily by DI, so it picks the window up from its registration
    static void TrackUnoWindow(this ShinyMediatorBuilder cfg, Window window)
        => cfg.Services.AddSingleton(new UnoWindowRegistration(window));
}
