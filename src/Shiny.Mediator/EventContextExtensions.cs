namespace Shiny.Mediator;


public static class EventContextExtensions
{
    public static T? TryGetValue<T>(this EventContext context, string key)
    {
        if (context.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
    
    internal static void MiddlewareException(
        this EventContext context, 
        Exception exception
    ) => context.Add("ExceptionHandlerEventMiddleware", exception);


    public static Exception? MiddlewareException(
        this EventContext context
    ) => context.TryGetValue<Exception>("ExceptionHandlerEventMiddleware");
}