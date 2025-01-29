using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Blazor.Infrastructure;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public static class BlazorExtensions
{
    /// <summary>
    /// Easier path to add Shiny Mediator to Blazor WebAssembly
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configAction"></param>
    /// <param name="includeStandardMiddleware"></param>
    /// <returns></returns>
    public static WebAssemblyHostBuilder AddShinyMediator(
        this WebAssemblyHostBuilder builder,
        Action<ShinyConfigurator>? configAction = null,
        bool includeStandardMiddleware = true
    )
    {
        builder.Services.AddShinyMediator(cfg =>
        {
            cfg.UseBlazor(includeStandardMiddleware);
            configAction?.Invoke(cfg);
        });
        return builder;
    }
    
    
    /// <summary>
    /// Add blazor internal services and component event collector
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="includeStandardMiddleware"></param>
    /// <returns></returns>
    public static ShinyConfigurator UseBlazor(this ShinyConfigurator cfg, bool includeStandardMiddleware = true)
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<BlazorEventCollector>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Browser")))
        {
            cfg.AddBlazorInfrastructure();
            if (includeStandardMiddleware)
                cfg.AddStandardAppSupportMiddleware();
        }
        return cfg;
    }


    public static ShinyConfigurator AddBlazorInfrastructure(this ShinyConfigurator cfg)
    {
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        
        return cfg;
    }
    
    
    /// <summary>
    /// Adds a file based caching service - ideal for cache surviving across app sessions
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddBlazorPersistentCache(this ShinyConfigurator configurator)
    {
        configurator.AddBlazorInfrastructure();
        configurator.AddCaching<StorageCacheService>();
        return configurator;
    }
}