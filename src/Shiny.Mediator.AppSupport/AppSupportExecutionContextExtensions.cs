namespace Shiny.Mediator;


public static class AppSupportExecutionContextExtensions
{
    public static OfflineAvailableContext? Offline(this IMediatorContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this IMediatorContext context, OfflineAvailableContext offlineContext)
        => context.AddHeader("Offline", offlineContext);
}