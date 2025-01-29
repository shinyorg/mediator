namespace Shiny.Mediator;


public static class AppSupportExecutionContextExtensions
{
    public static OfflineAvailableContext? Offline(this RequestContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this RequestContext context, OfflineAvailableContext offlineContext)
        => context.Add("Offline", offlineContext);
}