namespace Shiny.Mediator;


public static class AppSupportRequestContextExtensions
{
    public static Exception? UserErrorNotificationException(this IRequestContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotificationException), out var exception))
            return (Exception)exception;
        
        return null;
    }

    public static (string Title, string Message)? UserErrorNotificationAlert(this IRequestContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotificationAlert), out var exception))
            return ((string Title, string Message))exception;
        
        return null;
    }

    internal static void SetUserErrorNotificationException(this IRequestContext context, Exception exception)
        => context.Add(nameof(UserErrorNotificationException), exception);
    
    internal static void SetUserErrorNotificationAlert(this IRequestContext context, (string Title, string Message) alert)
        => context.Add(nameof(UserErrorNotificationAlert), alert);
}