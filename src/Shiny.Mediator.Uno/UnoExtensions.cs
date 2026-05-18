using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="IHostBuilder"/> and <see cref="ShinyMediatorBuilder"/> extensions that register
/// Shiny Mediator in Uno Platform apps along with Uno-specific infrastructure (Windows storage,
/// network information, popup dialogs) and the standard app-support middleware.
/// </summary>
public static class UnoExtensions
{
    /// <summary>
    /// Registers the standard app-support middleware and Uno infrastructure services
    /// (storage, internet, dialog) on the mediator builder.
    /// </summary>
    public static ShinyMediatorBuilder UseUno(this ShinyMediatorBuilder cfg)
    {
        cfg.AddStandardAppSupportMiddleware();
        cfg.AddUnoInfrastructure();
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
    /// Registers Shiny Mediator on an Uno <see cref="IHostBuilder"/>. When
    /// <paramref name="includeStandardMiddleware"/> is <c>true</c>, calls <see cref="UseUno"/>
    /// to install the Uno event/infrastructure services and the standard app-support middleware.
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
                configure?.Invoke(cfg);
                if (includeStandardMiddleware)
                    cfg.UseUno();
            }, 
            includeStandardMiddleware
        ));
        return builder;
    }


    /// <summary>
    /// Ensures the Uno infrastructure services required by Shiny Mediator middleware are
    /// registered: <see cref="IAlertDialogService"/> (Windows <c>MessageDialog</c>),
    /// <see cref="IInternetService"/> (Windows <c>NetworkInformation</c>), and
    /// <see cref="IStorageService"/> (Windows <c>ApplicationData.LocalFolder</c>).
    /// Uses <c>TryAdd</c> so existing registrations are preserved.
    /// </summary>
    public static ShinyMediatorBuilder AddUnoInfrastructure(this ShinyMediatorBuilder cfg)
    {
        // if (cfg.Services.Any(x => x.ImplementationType == typeof(UnoEventCollector)))
        //     return cfg;
        
        // cfg.Services.AddSingletonAsImplementedInterfaces<UnoEventCollector>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        return cfg;
    }
    
    
    // /// <summary>
    // /// Adds connectivity broadcaster
    // /// </summary>
    // /// <param name="configurator"></param>
    // /// <returns></returns>
    // public static ShinyConfigurator AddConnectivityBroadcaster(this ShinyConfigurator configurator)
    // {
    //     configurator.AddUnoInfrastructure();
    //     configurator.Services.AddSingleton<IServiceInitialize, ConnectivityBroadcaster>();
    //     return configurator;
    // }
    
    // /// <summary>
    // /// Adds a file based caching service - ideal for cache surviving across app sessions
    // /// </summary>
    // /// <param name="configurator"></param>
    // /// <returns></returns>
    // public static ShinyConfigurator AddPersistentCache(this ShinyConfigurator configurator)
    // {
    //     configurator.AddUnoInfrastructure();
    //     configurator.AddCaching<StorageCacheService>();
    //     return configurator;
    // }
}