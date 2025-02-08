using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

public static class Headers
{
    public static readonly (string Key, bool Value) BypassMiddleware = (nameof(BypassMiddleware), true);
    public static readonly (string Key, bool Value) BypassErrorTraps = (nameof(BypassErrorTraps), true);
    
    public static bool BypassMiddlewareEnabled(this IMediatorContext context)
        => context.Values.ContainsKey(BypassMiddleware.Key);
    
    public static bool BypassErrorTrapsEnabled(this IMediatorContext context)
        => context.Values.ContainsKey(BypassErrorTraps.Key);
    
    const string TimerRefreshHeader = nameof(TimerRefreshHeader);
    public static (string Key, int Value) TimerRefresh(int timerRefreshSeconds) 
        => (TimerRefreshHeader, timerRefreshSeconds);

    public static int? TryGetTimerRefresh(this RequestContext context)
        => context.TryGetValue<int>(TimerRefreshHeader);


    const string CommandScheduleHeader = nameof(CommandScheduleHeader);
    public static (string Key, object Value) SetCommandSchedule(DateTimeOffset dueAt)
        => (CommandScheduleHeader, dueAt);
    
    public static DateTimeOffset? TryGetCommandSchedule(this CommandContext context)
        => context.TryGetValue<DateTimeOffset>(CommandScheduleHeader);
    
    #region Caching
    
    const string CacheConfigHeader = nameof(CacheConfigHeader);
    public static CacheItemConfig? TryGetCacheConfig(this RequestContext context)
        => context.TryGetValue<CacheItemConfig>(CacheConfigHeader);
    
    public static (string Key, object Value) SetCacheConfig(this RequestContext context, CacheItemConfig cfg)
        => (CacheConfigHeader, cfg);
    
    
    const string ForceCacheRefreshHeader = nameof(ForceCacheRefreshHeader);
    public static (string Key, bool Value) ForceCacheRefresh { get; } = (ForceCacheRefreshHeader, true);

    public static bool HasForceCacheRefresh(this RequestContext context)
        => context.Values.ContainsKey(ForceCacheRefreshHeader);
    
    #endregion
}