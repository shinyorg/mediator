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
    
    
    public static OfflineAvailableContext? Offline(this ExecutionContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this ExecutionContext context, OfflineAvailableContext offlineContext)
        => context.Add("Offline", offlineContext);
}