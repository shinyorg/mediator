namespace Shiny.Mediator;


public static class EventExecutionContextExtensions
{
    public static T? TryGetValue<T>(this EventExecutionContext context, string key)
    {
        if (context.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
    
    internal static void MiddlewareException(
        this EventExecutionContext context, 
        Exception exception
    ) => context.Add("ExceptionHandlerEventMiddleware", exception);


    public static Exception? MiddlewareException(
        this EventExecutionContext context
    ) => context.TryGetValue<Exception>("ExceptionHandlerEventMiddleware");
}