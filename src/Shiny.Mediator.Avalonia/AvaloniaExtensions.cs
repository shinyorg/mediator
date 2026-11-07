using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// <see cref="ShinyMediatorBuilder"/> extensions that register Shiny Mediator in Avalonia apps along with
/// the Avalonia infrastructure (view event collection, main thread dispatch, storage, network
/// availability, alert dialogs) and the standard app-support middleware.
/// </summary>
public static class AvaloniaExtensions
{
    /// <summary>
    /// Registers the Avalonia event collector and, when <paramref name="includeStandardMiddleware"/> is
    /// <c>true</c>, the Avalonia infrastructure services along with the main thread and standard
    /// app-support middleware.
    /// </summary>
    /// <param name="cfg">The mediator builder.</param>
    /// <param name="includeStandardMiddleware">If <c>true</c>, main thread, user error notification, offline availability, and replay stream middleware is installed.</param>
    public static ShinyMediatorBuilder UseAvalonia(this ShinyMediatorBuilder cfg, bool includeStandardMiddleware = true)
    {
        if (!cfg.Services.Any(x => x.ImplementationType == typeof(AvaloniaEventCollector)))
            cfg.Services.AddSingletonAsImplementedInterfaces<AvaloniaEventCollector>();

        if (includeStandardMiddleware)
        {
            cfg.AddAvaloniaInfrastructure();
            cfg.AddMainThreadMiddleware<AvaloniaMainThreadDispatcher>();
            cfg.AddStandardAppSupportMiddleware();
        }
        return cfg;
    }


    /// <summary>
    /// Registers a <see cref="StorageCacheService"/> backed by the Avalonia file storage service so cached
    /// responses survive across app sessions. Also wires the Avalonia infrastructure services required by
    /// the cache store.
    /// </summary>
    public static ShinyMediatorBuilder AddAvaloniaPersistentCache(this ShinyMediatorBuilder cfg)
    {
        cfg.AddAvaloniaInfrastructure();
        cfg.AddCaching<StorageCacheService>();
        return cfg;
    }


    /// <summary>
    /// Ensures the Avalonia infrastructure services required by Shiny Mediator middleware are registered:
    /// <see cref="IMainThreadDispatcher"/> (<c>Dispatcher.UIThread</c>), <see cref="IAlertDialogService"/>,
    /// <see cref="IInternetService"/> (<c>NetworkChange</c>), and <see cref="IStorageService"/>
    /// (<c>LocalApplicationData</c>). Uses <c>TryAdd</c> so existing registrations are preserved.
    /// </summary>
    public static ShinyMediatorBuilder AddAvaloniaInfrastructure(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.TryAddSingleton<IMainThreadDispatcher, AvaloniaMainThreadDispatcher>();
        cfg.Services.TryAddSingleton<IAlertDialogService, AlertDialogService>();
        cfg.Services.TryAddSingleton<IInternetService, InternetService>();
        cfg.Services.TryAddSingleton<IStorageService, StorageService>();
        return cfg;
    }
}
