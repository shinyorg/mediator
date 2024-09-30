namespace Shiny.Mediator;


public static class AppSupportExecutionContextExtensions
{
    public static UserErrorNotificationContext? UserErrorNotification(this ExecutionContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotification), out var value))
            return (UserErrorNotificationContext)value;
        
        return null;
    }

    internal static void UserErrorNotification(this ExecutionContext context, UserErrorNotificationContext info)
        => context.Add(nameof(UserErrorNotification), info);
}