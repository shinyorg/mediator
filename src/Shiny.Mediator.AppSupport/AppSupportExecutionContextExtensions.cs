namespace Shiny.Mediator;


/// <summary>
/// Mediator context extensions for inspecting offline execution state added by the offline availability middleware.
/// </summary>
public static class AppSupportExecutionContextExtensions
{
    /// <summary>
    /// Returns the <see cref="OfflineAvailableContext"/> attached to the current execution when the result was served
    /// from offline storage, or <c>null</c> when the request was satisfied online.
    /// </summary>
    public static OfflineAvailableContext? Offline(this IMediatorContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");

    internal static void Offline(this IMediatorContext context, OfflineAvailableContext offlineContext)
        => context.AddHeader("Offline", offlineContext);
}