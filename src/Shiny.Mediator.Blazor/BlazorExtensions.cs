using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Blazor.Infrastructure;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="WebAssemblyHostBuilder"/> and <see cref="ShinyMediatorBuilder"/> extensions that
/// register Shiny Mediator in Blazor WebAssembly apps, wiring browser-backed infrastructure
/// (storage, internet, alert dialog) and the component-aware
/// <see cref="Shiny.Mediator.Blazor.Infrastructure.BlazorEventCollector"/>.
/// </summary>
public static class BlazorExtensions
{
    /// <summary>
    /// Registers Shiny Mediator on a Blazor WebAssembly host builder. Internally calls
    /// <see cref="UseBlazor"/> to install the Blazor event collector and, when running in the
    /// browser, the Blazor infrastructure services and standard app-support middleware.
    /// </summary>
    /// <param name="builder">The Blazor WebAssembly host builder.</param>
    /// <param name="configAction">Optional callback to register handlers and additional middleware.</param>
    /// <param name="includeStandardMiddleware">When <c>true</c> (default), installs the standard middleware stack.</param>
    public static WebAssemblyHostBuilder AddShinyMediator(
        this WebAssemblyHostBuilder builder,
        Action<ShinyMediatorBuilder>? configAction = null,
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
    /// Registers the Blazor component <see cref="Shiny.Mediator.Blazor.Infrastructure.BlazorEventCollector"/>
    /// and, when running in a browser (Blazor WebAssembly), the Blazor infrastructure services
    /// along with the standard app-support middleware. In Blazor Hybrid scenarios, infrastructure
    /// is left to the host platform.
    /// </summary>
    /// <param name="cfg">The mediator builder.</param>
    /// <param name="includeStandardMiddleware">When <c>true</c> (default), installs the standard middleware stack in WASM.</param>
    public static ShinyMediatorBuilder UseBlazor(this ShinyMediatorBuilder cfg, bool includeStandardMiddleware = true)
    {
        // this will work for MAUI and Blazor WASM
        cfg.Services.AddSingletonAsImplementedInterfaces<BlazorEventCollector>();

        if (OperatingSystem.IsBrowser())
        {
            // these should only be used in Blazor WASM
            // If hybrid, then these will be provided by the platform
            cfg.AddBlazorInfrastructure();
            if (includeStandardMiddleware)
                cfg.AddStandardAppSupportMiddleware();
        }
        return cfg;
    }


    /// <summary>
    /// Registers Blazor-specific infrastructure: a JS-interop-backed
    /// <see cref="IStorageService"/> (localStorage), <see cref="IInternetService"/>
    /// (navigator.onLine), and <see cref="IAlertDialogService"/> (browser <c>alert</c>).
    /// Uses <c>TryAdd</c> so existing registrations are preserved.
    /// </summary>
    public static ShinyMediatorBuilder AddBlazorInfrastructure(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        
        return cfg;
    }
    
    
    /// <summary>
    /// Registers a <see cref="StorageCacheService"/> backed by the Blazor JS-interop storage
    /// service so cached responses survive across browser sessions (localStorage).
    /// Also wires the Blazor infrastructure services required by the cache store.
    /// </summary>
    public static ShinyMediatorBuilder AddBlazorPersistentCache(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddBlazorInfrastructure();
        mediatorBuilder.AddCaching<StorageCacheService>();
        return mediatorBuilder;
    }
}