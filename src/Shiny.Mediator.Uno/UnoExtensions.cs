using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny.Mediator;


public static class UnoExtensions
{
    /// <summary>
    /// Add Default Uno Support to Mediator
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator UseUno(this ShinyConfigurator cfg)
    {                    
        cfg.AddStandardAppSupportMiddleware();
        cfg.AddUnoInfrastructure();
        return cfg;
    }
    
    /// <summary>
    /// Add Shiny Mediator to Uno
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <param name="includeStandardMiddleware"></param>
    /// <returns></returns>
    public static IHostBuilder AddShinyMediator(
        this IHostBuilder builder, 
        Action<ShinyConfigurator>? configure = null, 
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
    /// Adds necessary infrastructure for standard app middleware
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddUnoInfrastructure(this ShinyConfigurator cfg)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<UnoEventCollector>();
        
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        return cfg;
    }
    
    
    /// <summary>
    /// Adds connectivity broadcaster
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddConnectivityBroadcaster(this ShinyConfigurator configurator)
    {
        configurator.AddUnoInfrastructure();
        configurator.Services.AddSingleton<IServiceInitialize, ConnectivityBroadcaster>();
        return configurator;
    }
    
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