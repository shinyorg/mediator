namespace Shiny.Mediator;

public static class ContextExtensions
{
    
    
    
    public static TimeSpan? PerformanceLoggingThresholdBreached(this MediatorContext context)
        => context.TryGetValue<TimeSpan>("PerformanceLogging.Breach");

    internal static void SetPerformanceLoggingThresholdBreached(this MediatorContext context, TimeSpan elapsed)
        => context.Add("PerformanceLogging.Breach", elapsed);
    
    
    internal static void MiddlewareException(
        this MediatorContext context, 
        Exception exception
    ) => context.Add("ExceptionHandlerEventMiddleware", exception);


    public static Exception? MiddlewareException(
        this MediatorContext context
    ) => context.TryGetValue<Exception>("ExceptionHandlerEventMiddleware");
}