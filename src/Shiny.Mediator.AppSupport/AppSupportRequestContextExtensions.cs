namespace Shiny.Mediator;


public static class AppSupportRequestContextExtensions
{
    public static Exception? UserErrorNotificationException(this ExecutionContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotificationException), out var exception))
            return (Exception)exception;
        
        return null;
    }

    public static (string Title, string Message)? UserErrorNotificationAlert(this ExecutionContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotificationAlert), out var exception))
            return ((string Title, string Message))exception;
        
        return null;
    }

    internal static void SetUserErrorNotificationException(this ExecutionContext context, Exception exception)
        => context.Add(nameof(UserErrorNotificationException), exception);
    
    internal static void SetUserErrorNotificationAlert(this ExecutionContext context, (string Title, string Message) alert)
        => context.Add(nameof(UserErrorNotificationAlert), alert);
}