using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

public static class MediatorContextExtensions
{
    public static TimeSpan? PerformanceLoggingThresholdBreached(this IMediatorContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static IMediatorContext SetPerformanceLoggingThresholdBreached(this IMediatorContext context, TimeSpan elapsed)
    {
        context.AddHeader("PerformanceLogging.Breach", elapsed);
        return context;
    }


    const string TimerRefreshHeader = nameof(TimerRefreshHeader);

    public static IMediatorContext SetTimerRefresh(this IMediatorContext context, int timerRefreshSeconds)
    {
        context.AddHeader(TimerRefreshHeader, timerRefreshSeconds);
        return context;
    }

    public static int? TryGetTimerRefresh(this IMediatorContext context)
        => context.TryGetValue<int>(TimerRefreshHeader);


    const string CommandScheduleHeader = nameof(CommandScheduleHeader);
    public static IMediatorContext SetCommandSchedule(this IMediatorContext context, DateTimeOffset dueAt)
    {
        context.AddHeader(CommandScheduleHeader, dueAt);
        return context;
    }

    public static DateTimeOffset? TryGetCommandSchedule(this IMediatorContext context)
        => context.TryGetValue<DateTimeOffset>(CommandScheduleHeader);
    
    #region Caching
    
    const string CacheConfigHeader = nameof(CacheConfigHeader);
    public static CacheItemConfig? TryGetCacheConfig(this IMediatorContext context)
        => context.TryGetValue<CacheItemConfig>(CacheConfigHeader);

    public static IMediatorContext SetCacheConfig(this IMediatorContext context, CacheItemConfig cfg)
    {
        context.AddHeader(CacheConfigHeader, cfg);
        return context;
    }


    const string ForceCacheRefreshHeader = nameof(ForceCacheRefreshHeader);

    public static IMediatorContext ForceCacheRefresh(this IMediatorContext context)
    {
        context.AddHeader(ForceCacheRefreshHeader, true);
        return context;
    }

    public static bool HasForceCacheRefresh(this IMediatorContext context)
        => context.Headers.ContainsKey(ForceCacheRefreshHeader);
    
    #endregion
}