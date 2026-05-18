using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

/// <summary>
/// Helpers for reading and writing well-known headers on <see cref="IMediatorContext"/> used by built-in
/// middleware (performance logging, timer refresh, scheduled commands, and caching).
/// </summary>
public static class MediatorContextExtensions
{
    /// <summary>
    /// Returns the elapsed duration recorded when the performance logging middleware detected a threshold breach,
    /// or <c>null</c> if no breach occurred for this execution.
    /// </summary>
    public static TimeSpan? PerformanceLoggingThresholdBreached(this IMediatorContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static IMediatorContext SetPerformanceLoggingThresholdBreached(this IMediatorContext context, TimeSpan elapsed)
    {
        context.AddHeader("PerformanceLogging.Breach", elapsed);
        return context;
    }


    const string TimerRefreshHeader = nameof(TimerRefreshHeader);

    /// <summary>
    /// Sets the timer refresh interval (in seconds) used by <c>TimerRefreshStreamRequestMiddleware</c> for the current execution,
    /// overriding any attribute or configuration value.
    /// </summary>
    public static IMediatorContext SetTimerRefresh(this IMediatorContext context, int timerRefreshSeconds)
    {
        context.AddHeader(TimerRefreshHeader, timerRefreshSeconds);
        return context;
    }

    /// <summary>
    /// Reads the timer refresh interval (in seconds) previously set via <see cref="SetTimerRefresh"/>, or <c>null</c> if not set.
    /// </summary>
    public static int? TryGetTimerRefresh(this IMediatorContext context)
        => context.TryGetValue<int?>(TimerRefreshHeader);


    const string CommandScheduleHeader = nameof(CommandScheduleHeader);

    /// <summary>
    /// Schedules the current command for deferred execution at <paramref name="dueAt"/>. Picked up by the
    /// scheduled-command middleware to enqueue the command instead of running it immediately.
    /// </summary>
    public static IMediatorContext SetCommandSchedule(this IMediatorContext context, DateTimeOffset dueAt)
    {
        context.AddHeader(CommandScheduleHeader, dueAt);
        return context;
    }

    /// <summary>
    /// Reads the schedule due-date previously set via <see cref="SetCommandSchedule"/>, or <c>null</c> if not set.
    /// </summary>
    public static DateTimeOffset? TryGetCommandSchedule(this IMediatorContext context)
        => context.TryGetValue<DateTimeOffset?>(CommandScheduleHeader);

    #region Caching

    const string CacheConfigHeader = nameof(CacheConfigHeader);

    /// <summary>
    /// Reads a per-call <see cref="CacheItemConfig"/> previously set via <see cref="SetCacheConfig"/>, or <c>null</c> if not set.
    /// </summary>
    public static CacheItemConfig? TryGetCacheConfig(this IMediatorContext context)
        => context.TryGetValue<CacheItemConfig>(CacheConfigHeader);

    /// <summary>
    /// Overrides cache settings for the current execution. Takes precedence over configuration and handler attributes.
    /// </summary>
    public static IMediatorContext SetCacheConfig(this IMediatorContext context, CacheItemConfig cfg)
    {
        context.AddHeader(CacheConfigHeader, cfg);
        return context;
    }


    const string ForceCacheRefreshHeader = nameof(ForceCacheRefreshHeader);

    /// <summary>
    /// Forces the caching middleware to bypass any existing cached entry, execute the handler, and overwrite the cache.
    /// </summary>
    public static IMediatorContext ForceCacheRefresh(this IMediatorContext context)
    {
        context.AddHeader(ForceCacheRefreshHeader, true);
        return context;
    }

    /// <summary>
    /// Returns true when <see cref="ForceCacheRefresh"/> has been set on the context.
    /// </summary>
    public static bool HasForceCacheRefresh(this IMediatorContext context)
        => context.Headers.ContainsKey(ForceCacheRefreshHeader);

    #endregion
}
