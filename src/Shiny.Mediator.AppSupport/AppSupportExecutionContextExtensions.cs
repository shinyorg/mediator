namespace Shiny.Mediator;


public static class AppSupportExecutionContextExtensions
{
    public static UserErrorNotificationContext? UserErrorNotification(this RequestContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotification), out var value))
            return (UserErrorNotificationContext)value;
        
        return null;
    }

    internal static void UserErrorNotification(this RequestContext context, UserErrorNotificationContext info)
        => context.Add(nameof(UserErrorNotification), info);
    
    
    public static OfflineAvailableContext? Offline(this RequestContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this RequestContext context, OfflineAvailableContext offlineContext)
        => context.Add("Offline", offlineContext);
}