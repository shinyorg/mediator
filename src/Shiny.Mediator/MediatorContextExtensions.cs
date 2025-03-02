using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

public static class MediatorContextExtensions
{
    public static TimeSpan? PerformanceLoggingThresholdBreached(this IMediatorContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static void SetPerformanceLoggingThresholdBreached(this IMediatorContext context, TimeSpan elapsed)
        => context.AddHeader("PerformanceLogging.Breach", elapsed);
    
    
    const string TimerRefreshHeader = nameof(TimerRefreshHeader);
    public static void SetTimerRefresh(this IMediatorContext context, int timerRefreshSeconds) 
        => context.AddHeader(TimerRefreshHeader, timerRefreshSeconds);

    public static int? TryGetTimerRefresh(this IMediatorContext context)
        => context.TryGetValue<int>(TimerRefreshHeader);


    const string CommandScheduleHeader = nameof(CommandScheduleHeader);
    public static void SetCommandSchedule(this IMediatorContext context, DateTimeOffset dueAt)
        => context.AddHeader(CommandScheduleHeader, dueAt);
    
    public static DateTimeOffset? TryGetCommandSchedule(this IMediatorContext context)
        => context.TryGetValue<DateTimeOffset>(CommandScheduleHeader);
    
    #region Caching
    
    const string CacheConfigHeader = nameof(CacheConfigHeader);
    public static CacheItemConfig? TryGetCacheConfig(this IMediatorContext context)
        => context.TryGetValue<CacheItemConfig>(CacheConfigHeader);
    
    public static (string Key, object Value) SetCacheConfig(this IMediatorContext context, CacheItemConfig cfg)
        => (CacheConfigHeader, cfg);
    
    
    const string ForceCacheRefreshHeader = nameof(ForceCacheRefreshHeader);
    public static (string Key, bool Value) ForceCacheRefresh { get; } = (ForceCacheRefreshHeader, true);

    public static bool HasForceCacheRefresh(this IMediatorContext context)
        => context.Headers.ContainsKey(ForceCacheRefreshHeader);
    
    #endregion
}