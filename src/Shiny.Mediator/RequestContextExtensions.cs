namespace Shiny.Mediator;

public static class RequestContextExtensions
{
    public static T? TryGetValue<T>(this IRequestContext context, string key)
    {
        if (context.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
    
    
    public static TimeSpan? PerformanceLoggingThresholdBreached(this IRequestContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static void SetPerformanceLoggingThresholdBreached(this IRequestContext context, TimeSpan elapsed)
        => context.Add("PerformanceLogging.Breach", elapsed);
}