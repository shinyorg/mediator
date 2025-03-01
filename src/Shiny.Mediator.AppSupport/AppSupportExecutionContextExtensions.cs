namespace Shiny.Mediator;


public static class AppSupportExecutionContextExtensions
{
    public static OfflineAvailableContext? Offline(this MediatorContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this MediatorContext context, OfflineAvailableContext offlineContext)
        => context.Add("Offline", offlineContext);
}