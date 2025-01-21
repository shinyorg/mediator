namespace Shiny.Mediator;

public static class ContextExtensions
{
    internal static IMediatorContext PopulateHeaders(
        this IMediatorContext context,
        IEnumerable<(string Key, object Value)> headers
    )
    {
        foreach (var header in headers)
            context.Add(header.Key, header.Value);
        
        return context;
    }
    
    public static T? TryGetValue<T>(this IMediatorContext context, string key)
    {
        if (context.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
    
    
    public static TimeSpan? PerformanceLoggingThresholdBreached(this IMediatorContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static void SetPerformanceLoggingThresholdBreached(this IMediatorContext context, TimeSpan elapsed)
        => context.Add("PerformanceLogging.Breach", elapsed);
    
    
    internal static void MiddlewareException(
        this EventContext context, 
        Exception exception
    ) => context.Add("ExceptionHandlerEventMiddleware", exception);


    public static Exception? MiddlewareException(
        this EventContext context
    ) => context.TryGetValue<Exception>("ExceptionHandlerEventMiddleware");
}