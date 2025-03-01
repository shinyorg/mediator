using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;

public static class Headers
{
    public static readonly (string Key, bool Value) BypassMiddleware = (nameof(BypassMiddleware), true);
    public static readonly (string Key, bool Value) BypassExceptionHandling = (nameof(BypassExceptionHandling), true);
    
    public static bool BypassMiddlewareEnabled(this MediatorContext context)
        => context.Headers.ContainsKey(BypassMiddleware.Key);
    
    public static bool BypassExceptionHandlingEnabled(this MediatorContext context)
        => context.Headers.ContainsKey(BypassExceptionHandling.Key);
    
    const string TimerRefreshHeader = nameof(TimerRefreshHeader);
    public static (string Key, int Value) TimerRefresh(int timerRefreshSeconds) 
        => (TimerRefreshHeader, timerRefreshSeconds);

    public static int? TryGetTimerRefresh(this MediatorContext context)
        => context.TryGetValue<int>(TimerRefreshHeader);


    const string CommandScheduleHeader = nameof(CommandScheduleHeader);
    public static (string Key, object Value) SetCommandSchedule(DateTimeOffset dueAt)
        => (CommandScheduleHeader, dueAt);
    
    public static DateTimeOffset? TryGetCommandSchedule(this MediatorContext context)
        => context.TryGetValue<DateTimeOffset>(CommandScheduleHeader);
    
    #region Caching
    
    const string CacheConfigHeader = nameof(CacheConfigHeader);
    public static CacheItemConfig? TryGetCacheConfig(this MediatorContext context)
        => context.TryGetValue<CacheItemConfig>(CacheConfigHeader);
    
    public static (string Key, object Value) SetCacheConfig(this MediatorContext context, CacheItemConfig cfg)
        => (CacheConfigHeader, cfg);
    
    
    const string ForceCacheRefreshHeader = nameof(ForceCacheRefreshHeader);
    public static (string Key, bool Value) ForceCacheRefresh { get; } = (ForceCacheRefreshHeader, true);

    public static bool HasForceCacheRefresh(this MediatorContext context)
        => context.Headers.ContainsKey(ForceCacheRefreshHeader);
    
    #endregion
}