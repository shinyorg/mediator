namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class OfflineAvailableAttribute : Attribute;

public record OfflineAvailableFlushRequest : IRequest;

public record OfflineAvailableContext(
    string RequestKey,
    DateTimeOffset Timestamp
);

public static class OfflineExtensions
{
    public static OfflineAvailableContext? Offline(this ExecutionContext context)
        => context.TryGetValue<OfflineAvailableContext>("Offline");
    
    internal static void Offline(this ExecutionContext context, OfflineAvailableContext offlineContext)
        => context.Add("Offline", offlineContext);
}