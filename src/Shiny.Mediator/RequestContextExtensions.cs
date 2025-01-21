namespace Shiny.Mediator;

public static class RequestContextExtensions
{
    public static T? TryGetValue<T>(this RequestContext context, string key)
    {
        if (context.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
    
    
    public static TimeSpan? PerformanceLoggingThresholdBreached(this RequestContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static void SetPerformanceLoggingThresholdBreached(this RequestContext context, TimeSpan elapsed)
        => context.Add("PerformanceLogging.Breach", elapsed);
}