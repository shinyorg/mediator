using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


/// <summary>
/// Mediator builder extensions that register the app-support middleware bundle: user error notifications,
/// offline availability, and stream replay.
/// </summary>
public static class AppSupportExtensions
{
    /// <summary>
    /// Registers the full app-support middleware set: user error notifications, offline availability for requests,
    /// and replay caching for streams. Call this when wiring a mobile or desktop client.
    /// </summary>
    public static ShinyMediatorBuilder AddStandardAppSupportMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.AddUserErrorNotificationsHandling();
        cfg.AddOfflineAvailabilityMiddleware();
        cfg.AddReplayStreamMiddleware();
        return cfg;
    }


    /// <summary>
    /// Registers <see cref="UserNotificationExceptionHandler"/> so unhandled handler exceptions surface to the user
    /// via <see cref="IAlertDialogService"/> using localized titles and messages drawn from configuration.
    /// </summary>
    public static ShinyMediatorBuilder AddUserErrorNotificationsHandling(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.AddScoped<IExceptionHandler, UserNotificationExceptionHandler>();
        return cfg;
    }


    /// <summary>
    /// Registers the open-generic offline availability request middleware along with <see cref="IOfflineService"/>
    /// and the flush event handlers. Handlers marked with <see cref="OfflineAvailableAttribute"/> (or enabled via
    /// configuration) return stored results when the network is unavailable or the call times out.
    /// </summary>
    public static ShinyMediatorBuilder AddOfflineAvailabilityMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.Services.TryAddSingleton<IOfflineService, OfflineService>();
        cfg.Services.AddSingletonAsImplementedInterfaces<OfflineFlushEventHandlers>();
        cfg.AddOpenRequestMiddleware(typeof(OfflineAvailableRequestMiddleware<,>));

        return cfg;
    }


    /// <summary>
    /// Registers the open-generic stream replay middleware so stream handlers can yield the most recently stored
    /// value to subscribers immediately, before producing the next live value.
    /// </summary>
    public static ShinyMediatorBuilder AddReplayStreamMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.AddOpenStreamMiddleware(typeof(ReplayStreamMiddleware<,>));
        return cfg;
    }
}