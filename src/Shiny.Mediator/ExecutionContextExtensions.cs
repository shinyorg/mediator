namespace Shiny.Mediator;

public static class ExecutionContextExtensions
{
    public static T? TryGetValue<T>(this ExecutionContext context, string key)
    {
        if (context.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
    
    
    public static TimeSpan? PerformanceLoggingThresholdBreached(this ExecutionContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static void SetPerformanceLoggingThresholdBreached(this ExecutionContext context, TimeSpan elapsed)
        => context.Add("PerformanceLogging.Breach", elapsed);
}