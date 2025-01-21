namespace Shiny.Mediator;


public static class AppSupportExecutionContextExtensions
{
    public static UserErrorNotificationContext? UserErrorNotification(this IMediatorContext context)
    {
        if (context.Values.TryGetValue(nameof(UserErrorNotification), out var value))
            return (UserErrorNotificationContext)value;
        
        return null;
    }

    internal static void UserErrorNotification(this IMediatorContext context, UserErrorNotificationContext info)
        => context.Add(nameof(UserErrorNotification), info);
    
    public static OfflineAvailableContext? Offline(this IMediatorContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this RequestContext context, OfflineAvailableContext offlineContext)
        => context.Add("Offline", offlineContext);
}